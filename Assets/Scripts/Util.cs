using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public class Util
{

    //gets a float array that samples the waveform so a excerpt of res seconds is shown
    public static float[] GetTextureSamples(double timePerTexture, AudioClip audio, int texWidth)
    {
        //res / audio frame rate

        double visualSampleRate = timePerTexture / (double)texWidth;
        double audioSampleRate = 1.0 / ((double)audio.frequency * audio.channels);

        int stride = Math.Max(1, (int)Math.Floor(visualSampleRate / audioSampleRate));

        float[] output = new float[audio.samples / stride];
        float[] audioData = new float[audio.samples];

        audio.GetData(audioData, 0);
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = audioData[i * stride];
        }
        return output;

    }



    public static Texture2D PaintWaveformSpectrum(
       float[] samplesToVisualize,
       float timePerTexture,
       Texture2D tex,
       Song song,
       int width,
       int height,
       Color col,
       double startDSPTime,
       double beatMarkerDspTimeWidth)
    {
        double visualSampleRate = timePerTexture / (double)width;
        int startSample = (int)Math.Floor(startDSPTime / visualSampleRate);
        float[] waveform = new float[width];

        for (int i = startSample; i < samplesToVisualize.Length && i - startSample < width; i++)
        {
            waveform[i - startSample] = samplesToVisualize[i];
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(x, y, Color.black);
            }
        }

        Beat startBeat = song.beats.OrderBy(b => Math.Abs(b.dspTime - startDSPTime)).First();
        int startBeatIndex = song.beats.IndexOf(startBeat);
        int currBeatIndex = startBeatIndex;
        for (int x = 0; x < waveform.Length; x++)
        {
            int currSample = startSample + x;
            double currSampleDSPTime = currSample * visualSampleRate;
            if (currBeatIndex + 1 < song.beats.Count &&
                currSampleDSPTime >= song.beats[currBeatIndex].dspTime)
                currBeatIndex++;
            if (currBeatIndex < song.beats.Count &&
                DSPTimeInBeat(song, beatMarkerDspTimeWidth, currBeatIndex, currSampleDSPTime))
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, currBeatIndex % 2 == 0 ? Color.green : Color.yellow);
                }
            }
            else
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, (height / 2 + y), col);
                    tex.SetPixel(x, (height / 2 - y), col);
                }
            }

        }
        tex.Apply();
        return tex;
    }

    public static bool DSPTimeInBeat(Song song, double beatMarkerDspTimeWidth, int currBeatIndex, double currentDSPTime)
    {
        return  Math.Abs(song.beats[currBeatIndex].dspTime - currentDSPTime)/2.0 < beatMarkerDspTimeWidth;
    }
}


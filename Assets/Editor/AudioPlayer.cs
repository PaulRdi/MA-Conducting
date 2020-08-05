using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Audio;
using UnityEditor;
using System;
using System.Linq;
public enum AudioEditMode
{
    Idle,
    Playing,
    Recording,
    Editing
}

public class AudioEditor : EditorWindow
{
    Song song;
    AudioSource audioSource;
    double dspStart;
    int currBeat;
    int currBeatIndex;
    Texture2D tex;
    int width = 1000;
    int height = 200;
    float[] samplesToVisualize;
    float timePerTexture = 10.0f;
    AudioEditMode state;
    private void Update()
    {
        //Repaint();
        
    }

    [MenuItem("Window/Audio Editor")]
    public static void CreateAudioPlayer()
    {
        AudioEditor audioEditor = EditorWindow.GetWindow<AudioEditor>();
        audioEditor.Show();
        audioEditor.audioSource = FindObjectOfType<AudioSource>();
        audioEditor.state = AudioEditMode.Idle;
    }
    private void OnGUI()
    {
        HandleInput();

        GUILayout.BeginHorizontal();
        song = (Song)EditorGUILayout.ObjectField(song, typeof(Song));
        if (GUILayout.Button("Play"))
        {
            TryPlaySong();
        }
        if (GUILayout.Button("Stop"))
        {
            TryStopSong();
        }
        if (GUILayout.Button("Record"))
        {
            TryStartRecord();
        }
        if (GUILayout.Button("Edit"))
        {
            TryStartEdit();
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        Rect rect = new Rect();
        rect.Set(10, 40, 50, 50);

        if (state == AudioEditMode.Recording)
        {
            EditorGUI.DrawRect(rect, Color.red);
        }
        else if (state == AudioEditMode.Playing)
        {
            EditorGUI.DrawRect(rect, Color.green);
        }
        else if (state == AudioEditMode.Editing)
        {
            EditorGUI.DrawRect(rect, Color.yellow);
        }
        else
        {
            EditorGUI.DrawRect(rect, Color.black);

        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (state == AudioEditMode.Playing)
        {

            double currDSP = AudioSettings.dspTime - dspStart;
            if (currBeat < song.beats.Count &&
               currDSP > song.beats[currBeat].dspTime)
            {
                currBeat++;
            }
            if (currBeat < song.beats.Count)
            {
                GUILayout.Label(song.beats[currBeat].beat.ToString());
            }
            GUILayout.EndHorizontal();
            DrawWaveformTextureAtDSPTime(currDSP);
        }
        else if (state == AudioEditMode.Playing ||
                 state == AudioEditMode.Recording)
        {
            GUILayout.Label(currBeat.ToString());
            GUILayout.EndHorizontal();
        }
        else if (state == AudioEditMode.Editing)
        {
            GUILayout.EndHorizontal();
            DrawWaveformTextureAtDSPTime(10.0);
        }
    }

    private void DrawWaveformTextureAtDSPTime(double dspTime)
    {
        GUILayout.BeginHorizontal();
        EditorGUI.DrawPreviewTexture(new Rect(0, 110, width, height),
            PaintWaveformSpectrum(width, height, Color.grey, dspTime));
        GUILayout.EndHorizontal();
    }

    private void TryStartEdit()
    {
        TryStopSong();
        if (song != null)
        {
            state = AudioEditMode.Editing;
            CreateSamplesAndTexture();
        }
    }

    private void HandleInput()
    {
        if (state == AudioEditMode.Recording &&
                    Event.current != null &&
                    Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.G)
        {
            double currDSP = AudioSettings.dspTime - dspStart;
            song.beats.Add(new Beat(currBeat, currDSP));
            currBeat = (currBeat + 1) % 4;

        }
    }

    private void TryStartRecord()
    {
        TryStopSong();
        currBeat = 0;
        TryPlaySong();
        song.beats = new List<Beat>();
        state = AudioEditMode.Recording;
    }

    public void TryStopSong()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        if (state == AudioEditMode.Recording)
        {
            Undo.RecordObject(song, "Added beats to song");
            EditorUtility.SetDirty(song);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        if (tex != null)
            DestroyImmediate(tex);
        state = AudioEditMode.Idle;
    }
    private void TryPlaySong()
    {
        TryStopSong();
        if (song != null)
        {
            dspStart = AudioSettings.dspTime;
            if (audioSource == null)
            {
                audioSource = new GameObject().AddComponent<AudioSource>();
                audioSource.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            audioSource.Stop();
            audioSource.PlayOneShot(song.audioClip);
            currBeatIndex = 0;
            currBeat = 0;
            state = AudioEditMode.Playing;
            CreateSamplesAndTexture();

        }
    }

    private void CreateSamplesAndTexture()
    {
        samplesToVisualize = GetTextureSamples(this.timePerTexture, song.audioClip, 1000);
        tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
    }

    //gets a float array that samples the waveform so a excerpt of res seconds is shown
    public float[] GetTextureSamples(double timePerTexture, AudioClip audio, int texWidth)
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
    //https://answers.unity.com/questions/189886/displaying-an-audio-waveform-in-the-editor.html
    //modified to paint part of song
    //https://docs.unity3d.com/ScriptReference/AudioSettings-dspTime.html

    public Texture2D PaintWaveformSpectrum(
        int width, 
        int height, 
        Color col, 
        double startDSPTime)
    {
        double visualSampleRate = timePerTexture / (double)width;
        int startSample = (int)Math.Floor(startDSPTime / visualSampleRate);
        float[] waveform = new float[width];

        for (int i = startSample; i < samplesToVisualize.Length && i-startSample < width; i++)
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
                Math.Abs(currSampleDSPTime - song.beats[currBeatIndex].dspTime) < .015)
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
}

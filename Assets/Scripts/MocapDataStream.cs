using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;

public class MocapDataStream
{
    public MoCapData data;
    public double currDSP;
    public MocapDataStream(string jsonMocapData)
    {
        data = JsonConvert.DeserializeObject<MoCapData>(jsonMocapData);
    }
    public IEnumerator Play(Transform[] controllingTransforms)
    {
        int currentFrame = 0;
        double startDSPTime = AudioSettings.dspTime;

        while (currentFrame < data.dspTimeToMoCapFrame.Keys.Count)
        {
            currDSP = AudioSettings.dspTime 
                           - startDSPTime 
                           + data.firstBeatRelativeDSPTime 
                           - TestConfig.current.startOffset;

            int n = 0;
            while (data.dspTimeToMoCapFrame[currentFrame].relativeDspTime < currDSP)
            {
                currentFrame++; //catch up if frame rate of player is slower than recorded frame rate.
                n++;
            }                   //wait if frame rate of player is faster than recorded frame rate.

            if (n > 0)
                Debug.Log("Catch up: " + n + " frames");
            for (int i = 0; i < controllingTransforms.Length; i++)
            {
                controllingTransforms[i].position = data.dspTimeToMoCapFrame[currentFrame][i];
            }
            yield return null;
        }
        yield return null;
    }
}


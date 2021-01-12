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
    public int currentFrame { get; set; }

    public MocapDataStream(string jsonMocapData)
    {
        data = JsonConvert.DeserializeObject<MoCapData>(jsonMocapData);
        currentFrame = 0;
    }
    public IEnumerator Play(Transform[] controllingTransforms)
    {
        double startDSPTime = AudioSettings.dspTime;
        currentFrame = 0;
        while (currentFrame < data.dspTimeToMoCapFrame.Keys.Count)
        {
            currDSP = AudioSettings.dspTime 
                           - startDSPTime 
                           + data.firstBeatRelativeDSPTime 
                           - TestConfig.current.startOffset;

            int n = 0;
            while (currentFrame < data.dspTimeToMoCapFrame.Keys.Count-1 &&
                data.dspTimeToMoCapFrame[currentFrame].relativeDspTime < currDSP)
            {
                currentFrame++; //catch up if frame rate of player is slower than recorded frame rate.
                n++;
            }                   //wait if frame rate of player is faster than recorded frame rate.

            //if (n > 0)
            //    Debug.Log("Catch up: " + n + " frames");
            UpdateControllingTransforms(controllingTransforms);
            yield return null;
        }
        yield return null;
    }

    public void UpdateControllingTransforms(Transform[] controllingTransforms)
    {
        for (int i = 0; i < controllingTransforms.Length; i++)
        {
            controllingTransforms[i].position = data.dspTimeToMoCapFrame[currentFrame][i];
        }
    }
}


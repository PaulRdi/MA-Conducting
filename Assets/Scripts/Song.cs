using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PhaseSpace.OWL;

[CreateAssetMenu(fileName = "song.asset")]
public class Song : ScriptableObject
{
    public AudioClip audioClip;
    public List<Beat> beats;
    public double firstBeatTime => beats[0].dspTime;
    
}
[Serializable]
public class Beat
{
    public int beat;
    public double dspTime;

    public Beat(int beat, double dspTime)
    {
        this.beat = beat;
        this.dspTime = dspTime;
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class IngameRecorder : MonoBehaviour
{
    [SerializeField]
    Song song;
    [SerializeField] AudioSource audioSource;
    bool recording = false;
    double startDSP;
    int currBeat;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!recording)
                StartRecording();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            if (recording)
                StopRecording();
        }
        if (recording)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                RecordBeat();
            }
        }
    }

    private void RecordBeat()
    {
        currBeat = (currBeat + 1)%4;
        song.beats.Add(new Beat(currBeat, AudioSettings.dspTime - startDSP));
    }

    private void StopRecording()
    {
        recording = false;
        audioSource.Stop();
    }

    private void StartRecording()
    {
        song.beats = new List<Beat>();
        recording = true;
        currBeat = 0;
        startDSP = AudioSettings.dspTime;
        audioSource.clip = song.audioClip;
        audioSource.Play();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;
using System;

public class TestManager : MonoBehaviour
{
    [SerializeField] Song song;
    [SerializeField] AudioSource audioSource;

    BeatBarController beatBarController;
    int currBeat;
    int totalBeats;
    bool songRunning;
    double startDSPTime;
    double currBeatStartTime;
    double lastDSPTime;
    double dspDelta;
    double timeOnCurrentBeat;
    private void Awake()
    {
        songRunning = false;
        beatBarController = FindObjectOfType<BeatBarController>();
        dspDelta = 0;
        lastDSPTime = 0;
        startDSPTime = 0;
        currBeatStartTime = 0;
        currBeat = 0;
        totalBeats = 0;
    }
    private void Start()
    {
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            StartSong();
        if (songRunning)
            UpdateRunning();
    }

    private void UpdateRunning()
    {
        double dspTime = AudioSettings.dspTime;
        double dspDiff = song.beats[totalBeats].dspTime - dspTime;
        timeOnCurrentBeat += dspDelta;

        if (dspDiff <= 0)
        {
            NextBeat(dspTime, dspDiff);
        }
        beatBarController.timeOnCurrentBeat = timeOnCurrentBeat;

        lastDSPTime = dspTime;
    }

    private void NextBeat(double dspTime, double dspDiff)
    {
        totalBeats = Math.Min(totalBeats + 1, song.beats.Count - 1);
        currBeat = totalBeats % 4; // because song is in 4/4
        currBeatStartTime = dspTime;
        double relativeBeatLength = song.beats[currBeat + 1].dspTime - song.beats[currBeat].dspTime;
        beatBarController.timeToNextBeat = relativeBeatLength;
        beatBarController.currBeat = currBeat;
        timeOnCurrentBeat = 0;

    }

    void StartSong()
    {
        audioSource.clip = song.audioClip;
        audioSource.Play();
        startDSPTime = AudioSettings.dspTime;
        currBeat = 0;
        totalBeats = 0;
        songRunning = true;
    }
}

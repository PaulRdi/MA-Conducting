using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;
using System;
using UnityEngine.UI;
using PhaseSpace.Unity;

public class TestManager : MonoBehaviour
{
    public static event Action correctBeatDetected;
    public static event Action falseBeatDetected;
    public static event Action testStarted;

    [SerializeField] bool useRigid = true;
    [SerializeField] Song song;
    [SerializeField] AudioSource audioSource;
    [SerializeField] TestConfig config;
    [SerializeField] Transform phasespace;
    [SerializeField] Transform rig;
    [SerializeField] Button calibrationButton;
    [SerializeField] Transform calibrationPoint;
    [SerializeField] Transform closeScaleTransform, farScaleTransform;
    [SerializeField] OWLMarker rightHandMarker, leftHandMarker;
    [SerializeField] OWLRigidbody rightHandRigid;
    [SerializeField] Transform rightHandIKRef;
    Vector3 rightHandInitialdelta;
    BeatBarController beatBarController;
    int currBeat;
    int totalBeats;
    bool songRunning;
    bool beatsRunning;
    double startDSPTime;
    double currBeatStartTime;
    double lastDSPTime;
    double dspDelta;
    double relativeBeatLength;
    private bool calibrated;

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
        TestConfig.current = config;
        calibrationButton.onClick.AddListener(Calibrate);

    }
    private void Calibrate()
    {
        Vector3 dir = farScaleTransform.position - closeScaleTransform.position;
        if (!useRigid)
            rightHandInitialdelta = rightHandIKRef.position - rightHandMarker.transform.position;
        else
            rightHandInitialdelta = rightHandIKRef.position - rightHandRigid.transform.position;
        calibrated = true;
    }

    IEnumerator TestRoutine()
    {
        yield return WaitForCalibration();
        
    }

    IEnumerator WaitForCalibration()
    {
        calibrated = false;
        yield return new WaitUntil(() => calibrated);
    }

    private void Update()
    {
        if (rightHandInitialdelta != default)
        {
            UpdateIKRefs();
        }
         
        if (UnityEngine.Input.GetKeyDown(KeyCode.F) &&
            !songRunning)
            StartSong();
        if (beatsRunning)
        {
            UpdateBeats();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                BeatDetected();
        }
    }

    private void UpdateIKRefs()
    {
        if (!useRigid)
            rightHandIKRef.transform.position = rightHandMarker.transform.position + rightHandInitialdelta;
        else
            rightHandIKRef.transform.position = rightHandRigid.transform.position + rightHandInitialdelta;
    }

    private void UpdateBeats()
    {

        double dspTime = AudioSettings.dspTime - startDSPTime;
        dspDelta = dspTime - lastDSPTime;
        double dspDiff = song.beats[totalBeats].dspTime - dspTime;
        
        if (dspDiff < 0)
        {
            NextBeat(dspTime, dspDiff);
        }        
        beatBarController.timeOnCurrentBeat = relativeBeatLength - dspDiff;
        lastDSPTime = dspTime;
    }

    private void NextBeat(double dspTime, double dspDiff)
    {
        totalBeats = Math.Min(totalBeats + 1, song.beats.Count - 1);
        currBeat = totalBeats % 4; // because song is in 4/4
        UpdateCurrentBeatValues(dspTime);

    }

    private void UpdateCurrentBeatValues(double dspTime)
    {
        currBeatStartTime = dspTime;
        relativeBeatLength = song.beats[totalBeats + 1].dspTime - song.beats[totalBeats].dspTime;
        beatBarController.timeToNextBeat = relativeBeatLength;
        beatBarController.currBeat = currBeat;
        Debug.Log(currBeat);
    }
    void StopSong()
    {
        beatsRunning = false;
        songRunning = false;
    }
    void StartSong()
    {
        audioSource.clip = song.audioClip;
        audioSource.Play();
        startDSPTime = AudioSettings.dspTime;
        currBeat = 0;
        totalBeats = 0;
        songRunning = true;
        beatsRunning = false;
        beatBarController.startDspTime = startDSPTime;
        beatBarController.timeToNextBeat = song.beats[0].dspTime;
        StartCoroutine(WaitForBeatsStart());
        testStarted?.Invoke();  
    }
    IEnumerator WaitForBeatsStart()
    {
        double currDsp = AudioSettings.dspTime - startDSPTime;
        while (song.beats[0].dspTime - currDsp > 0)
        {
            currDsp = AudioSettings.dspTime - startDSPTime;
            yield return null;
        }
        UpdateCurrentBeatValues(currDsp);
        beatsRunning = true;
    }
    //check if detected beat was in timeframe
    void BeatDetected()
    {
        double dsp = AudioSettings.dspTime - startDSPTime;

        if (Math.Abs(song.beats[totalBeats].dspTime - dsp) <= TestConfig.current.beatBuffer / 2 ||
            Math.Abs(song.beats[totalBeats+1].dspTime - dsp) <= TestConfig.current.beatBuffer / 2)
        {
            correctBeatDetected?.Invoke();
            Debug.Log("correct");
        }
        else
        {
            falseBeatDetected?.Invoke();
            Debug.Log("false");

        }

    }
}

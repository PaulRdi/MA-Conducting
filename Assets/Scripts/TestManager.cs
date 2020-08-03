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
    double startDSPTime;
    double currBeatStartTime;
    double lastDSPTime;
    double dspDelta;
    double timeOnCurrentBeat;
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
         
        if (UnityEngine.Input.GetKeyDown(KeyCode.F))
            StartSong();
        if (songRunning)
        {
            UpdateRunning();
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

    private void UpdateRunning()
    {

        double dspTime = AudioSettings.dspTime - startDSPTime;
        dspDelta = dspTime - lastDSPTime;
        double dspDiff = song.beats[totalBeats + 1].dspTime - dspTime;

        if (dspDiff <= 0)
        {
            NextBeat(dspTime, dspDiff);
        }

        timeOnCurrentBeat += dspDelta;
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

        testStarted?.Invoke();
    }
    //check if detected beat was in timeframe
    void BeatDetected()
    {
        double dsp = AudioSettings.dspTime - startDSPTime;

        if (Math.Abs(song.beats[totalBeats].dspTime - dsp) <= TestConfig.current.beatBuffer ||
            Math.Abs(song.beats[totalBeats+1].dspTime - dsp) <= TestConfig.current.beatBuffer)
        {
            correctBeatDetected?.Invoke();
        }
        else
        {
            falseBeatDetected?.Invoke();
        }

    }
}

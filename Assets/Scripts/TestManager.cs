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
    public static event Action<Transform, Transform> onCalibrate; //pushes hip transforms (marker hip, then rig hip)

    public static TestManager instance;

    [SerializeField] Song song;
    [SerializeField] AudioSource audioSource;
    [SerializeField] TestConfig config;
    [SerializeField] Transform phasespace;
    [SerializeField] Transform rig;
    [SerializeField] Button calibrationButton;
    [SerializeField] Transform calibrationPoint;
    [SerializeField] Transform closeScaleTransform, farScaleTransform;
    [SerializeField] Transform rightHandIKRef;
    [SerializeField] Transform leftHandIKRef;
    [SerializeField] Transform rightHandTransform, leftHandTransform;
    [SerializeField] Transform markerHip, rigHip;
    [SerializeField] Animator rigAnimator;
    [SerializeField] RawImage waveformPreviewImage;
    Texture2D waveformPreviewTexture;
    Vector3 rightHandInitialDelta;
    Vector3 leftHandInitialDelta;
    BeatBarController beatBarController;
    int currBeat;
    int totalBeats;
    public bool songRunning;
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
        instance = this;
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

    void Calibrate()
    {
        CalibrateIK();
        calibrated = true;
        onCalibrate?.Invoke(markerHip, rigHip);
    }

    private void CalibrateIK()
    {
        Vector3 rigTposeVector = rightHandIKRef.transform.position - leftHandIKRef.transform.position;
        Vector3 suitTposeVector = rightHandTransform.position - leftHandTransform.position;

        Quaternion tposeRotation = Quaternion.FromToRotation(rigTposeVector, suitTposeVector);

        float scale = suitTposeVector.magnitude / rigTposeVector.magnitude;
        
        rig.transform.localScale *= scale;
        rig.transform.Rotate(Vector3.up, tposeRotation.eulerAngles.y);
        
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
        if (UnityEngine.Input.GetKeyDown(KeyCode.F) &&
            !songRunning)
            StartSong();
        if (beatsRunning)
        {
            UpdateBeats();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                BeatDetected();
            DrawSongPreview();
        }
    }

    private void DrawSongPreview()
    {
       float[] samples = Util.GetTextureSamples(1.0, song.audioClip, (int)waveformPreviewImage.rectTransform.rect.width);
        Util.PaintWaveformSpectrum(
            samples,
            1.0f,
            waveformPreviewTexture,
            song,
            (int)waveformPreviewImage.rectTransform.rect.width,
            (int)waveformPreviewImage.rectTransform.rect.height,
            Color.grey,
            AudioSettings.dspTime - startDSPTime,
            TestConfig.current.beatBuffer);
    }

    private void UpdateBeats()
    {
        if (totalBeats >= song.beats.Count)
            return;
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
    }
    void StopSong()
    {
        beatsRunning = false;
        songRunning = false;
    }
    void StartSong()
    {
        if (waveformPreviewTexture != null)
            DestroyImmediate(waveformPreviewTexture);

        waveformPreviewTexture = new Texture2D((int)waveformPreviewImage.rectTransform.rect.width, (int)waveformPreviewImage.rectTransform.rect.height);
        waveformPreviewImage.texture = waveformPreviewTexture;
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
        if ((totalBeats < song.beats.Count &&
            Util.DSPTimeInBeat(song, TestConfig.current.beatBuffer, totalBeats, dsp)) ||
            (totalBeats+1 < song.beats.Count &&
            Util.DSPTimeInBeat(song, TestConfig.current.beatBuffer, totalBeats+1, dsp)))
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

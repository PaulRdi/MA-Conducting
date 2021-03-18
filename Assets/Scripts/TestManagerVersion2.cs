using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using PhaseSpace;
using PhaseSpace.Unity;
using System;
using UnityEngine.UI;
using TMPro;
public enum DataState
{
    NotLoaded,
    LoadingData,
    Loaded
}
public enum TestState
{
    Idle,
    Initializing,
    Running
}
public class TestManagerVersion2 : MonoBehaviour
{
    public static TestManagerVersion2 instance;
    public static event Action dataLoaded;
    public static event Action<double> tick;
    public static event Action<MarkerGroup, Transform> recorded_onCalibrate;
    public static event Action<MarkerGroup, Transform> realtime_onCalibrate;

    [SerializeField] TestConfig config;
    [SerializeField] string accuraciesFileName = "accuracies.csv";
    [SerializeField] MotionRecording recording1;
    [SerializeField] MotionRecording recording2;
    [SerializeField] Song song;

    [SerializeField] Transform recordedSuit_rightHandIKRef, recordedSuit_leftHandIKRef;
    [SerializeField] MarkerGroup recordedSuit_rightHand, recordedSuit_leftHand;
    [SerializeField] Transform realtimeVis_leftHand, realtimeVis_rightHand;
    [SerializeField] MarkerGroup realtimeSuit_rightHand, realtimeSuit_leftHand;
    [SerializeField] Transform recorded_RigHip, realtime_RigHip;
    [SerializeField] Transform rigParent;
    [SerializeField] MarkerGroup recordedSuit_markerHip, realtimeSuit_markerHip;
    [SerializeField] Animator rigAnimator;

    [SerializeField] Button tryStartTestButton;
    [SerializeField] GameObject loadingDataPanel;
    [SerializeField] GameObject loadingDataErrorPanel;
    [SerializeField] TextMeshProUGUI loadingDataErrorText;
    [SerializeField] Image accuracyBar;

    [SerializeField] AudioSource audioSource;
    [SerializeField] ParticleSystem beatHitParticles;
    [SerializeField] AccuracyTester accuracyTester;

    public bool manualTick = false;
    bool tickKeyPressed;

    public DataState dataState { get; private set; }
    public TestState testState { get; private set; }
    Coroutine loadRoutine;
    Coroutine testRoutine;

    double startDSPTimeMotion;
    double currentDSPTimeMotion;
    double lastDspTimeMotion;

    double currentDSPTimeSong;
    double startDSPTimeSong;

    Beat currentBeat;
    bool currentBeatHit;
    Beat lastBeat;
    int beatIndex;

    public double currentAccuracy;
    public List<double> recordedAccuracies;
    public List<double> recordedAccuracyRow;
    private void OnValidate()
    {
        TestConfig.current = config;
    }

    void Awake()
    {
        dataState = DataState.NotLoaded;
        testState = TestState.Idle;
        instance = this;
        recordedAccuracies = new List<double>();
        recordedAccuracyRow = new List<double>();
        TestConfig.current = config;
        TryLoadData(TryStartTest);
    }

    private void SetIdleTestState()
    {
        testState = TestState.Idle;
    }

    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
            tickKeyPressed = true;
        else if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
            WriteAccuracies();
        UpdateAccuracy();
    }

    private void WriteAccuracies()
    {
        string s = "";
        foreach (double val in recordedAccuracyRow)
            s += val.ToString() + "\n";

        File.WriteAllText(Application.streamingAssetsPath + "/" + accuraciesFileName, s);
    }

    private void LateUpdate()
    {
        lastDspTimeMotion = AudioSettings.dspTime;
    }
    void TryStartTest()
    {
        if (dataState != DataState.Loaded)
            return;
        if (testRoutine == null)
        {
            testRoutine = StartCoroutine(TestRoutine());
        }
    }

    void TryLoadData(Action finishSuccessfulAction)
    {
        if (recording1 == default ||
            recording2 == default)
        {
            Debug.LogError("Motion recording missing in TestManagerVersion2");
            return;
        }
        if (loadRoutine == null &&
            File.Exists(FullMotionTrackingDataCapturer.path + recording1.fileName) &&
            File.Exists(FullMotionTrackingDataCapturer.path + recording2.fileName))
            loadRoutine = StartCoroutine(LoadRoutine(finishSuccessfulAction));
        else
            Debug.LogError("Could not find a recording file. Please assign the correct file names to the config files.");

    }

    IEnumerator TestRoutine()
    {
        InitTest();
        audioSource.PlayOneShot(song.audioClip);
        while (currentDSPTimeSong < song.beats[0].dspTime)
        {
            currentDSPTimeSong = AudioSettings.dspTime - startDSPTimeSong;
            yield return null;
        }
        currentAccuracy = 1.0;
        startDSPTimeMotion = AudioSettings.dspTime;
        while (currentDSPTimeMotion < 300.0d)
        {
            if (!manualTick)
            {
                currentDSPTimeMotion = AudioSettings.dspTime - startDSPTimeMotion;
                currentDSPTimeSong = AudioSettings.dspTime - startDSPTimeSong;
                yield return null;
            }
            else
            {
                currentDSPTimeMotion += AudioSettings.dspTime - lastDspTimeMotion;
                yield return new WaitUntil(() => tickKeyPressed);
            }
            if (lastDspTimeMotion != AudioSettings.dspTime)
                tick?.Invoke(AudioSettings.dspTime - lastDspTimeMotion);
            tickKeyPressed = false;
            recordedAccuracyRow.Add(currentAccuracy);
            if (currentBeat.dspTime - currentDSPTimeSong < TestConfig.current.beatBuffer / 2.0)
            {
                if (beatIndex + 1 < song.beats.Count)
                {
                    beatIndex++;
                    lastBeat = currentBeat;
                    currentBeat = song.beats[beatIndex];
                    if (!currentBeatHit)
                    {
                        currentAccuracy -= TestConfig.current.missedBeatPenalty;
                    }
                    else
                    {
                        Instantiate(beatHitParticles, realtimeVis_leftHand.position, Quaternion.identity);
                        Instantiate(beatHitParticles, realtimeVis_rightHand.position, Quaternion.identity);
                    }
                    currentBeatHit = false;
                }
            }
        }
        StopTest();
    }


    private void InitTest()
    {
        testState = TestState.Initializing;
        currentAccuracy = 1.0;
        currentDSPTimeMotion = 0.0d;
        startDSPTimeMotion = AudioSettings.dspTime;
        currentDSPTimeSong = 0.0d;
        startDSPTimeSong = AudioSettings.dspTime;
        TryGetMoCapFrame(recording1, out FullMocapFrame frame0_rec1, 0);
        TryGetMoCapFrame(recording2, out FullMocapFrame frame0_rec2, 0);
        recordedSuit_rightHand.ForceMeasurement(0);
        recordedSuit_leftHand.ForceMeasurement(0);
        recordedSuit_markerHip.ForceMeasurement(0);
        realtimeSuit_leftHand.ForceMeasurement(0);
        realtimeSuit_rightHand.ForceMeasurement(0);
        realtimeSuit_markerHip.ForceMeasurement(0);
        rigAnimator.enabled = false;
        Physics.SyncTransforms();
        Util.CalibrateIK(
            recordedSuit_rightHandIKRef,
            recordedSuit_leftHandIKRef,
            recordedSuit_rightHand.controllingTransform,
            recordedSuit_leftHand.controllingTransform,
            rigParent);
        recorded_onCalibrate?.Invoke(recordedSuit_markerHip, recorded_RigHip);
        realtime_onCalibrate?.Invoke(realtimeSuit_markerHip, realtime_RigHip);
        rigAnimator.enabled = true;
        testState = TestState.Running;
        currentBeat = song.beats[0];
        BeatDetector.beatDetected += BeatDetector_beatDetected;
    }

    IEnumerator LoadRoutine(Action finishSuccessfulAction)
    {
        dataState = DataState.LoadingData;
        loadingDataPanel.gameObject.SetActive(true);
        Debug.Log("Data loading: <color=#ffaa00>start</color>");

        Task.Factory.StartNew(() => LoadData())
            .ContinueWith(
                t =>
                {
                    if (t.IsFaulted)
                    {
                        var errorMessage = "Data Loading: <color=#ff0000>error</color> \n";
                        t.Exception.Flatten().Handle(e =>
                        {
                            errorMessage += e.ToString();
                            dataState = DataState.NotLoaded;
                            return true;
                        });
                        loadingDataErrorPanel.SetActive(true);
                        loadingDataErrorText.text = errorMessage;
                        StopDataLoading();
                    }
                    else
                    {
                        dataState = DataState.Loaded;
                    }
                }, 
                TaskScheduler.FromCurrentSynchronizationContext());

        yield return new WaitUntil(() => dataState == DataState.Loaded);
        finishSuccessfulAction?.Invoke();
        dataLoaded?.Invoke();
        loadingDataPanel.gameObject.SetActive(false);
        loadRoutine = null;
        Debug.Log("Data loading: <color=#00ff00>success</color>");
    }

    private void UpdateAccuracy()
    {
        int pdc = TestConfig.current.particleDeathCutoff;
        double overExpectedParticleDeaths = Math.Max(0, accuracyTester.lastAverage - pdc);
        double underExpectedParticleDeaths = Math.Max(0, -(accuracyTester.lastAverage - pdc));
        double maxParticleDeathsOverExpectation = TestConfig.current.numParticles - pdc;
        double maxParticleDeathsUnderExpectation = TestConfig.current.numParticles - maxParticleDeathsOverExpectation;
        double overestimatedParticleDeathRatio = overExpectedParticleDeaths / maxParticleDeathsOverExpectation;
        double underestimatedParticleDeathRatio = underExpectedParticleDeaths / maxParticleDeathsUnderExpectation;

        currentAccuracy -= overestimatedParticleDeathRatio * TestConfig.current.maxAccuracyLossPerSecond * Time.deltaTime;
        currentAccuracy += underestimatedParticleDeathRatio * TestConfig.current.maxAccuracyGainPerSecond * Time.deltaTime;

        currentAccuracy = Math.Min(1.0, Math.Max(0.0, currentAccuracy));

        accuracyBar.fillAmount = (float)currentAccuracy;

        if (recordedAccuracies.Count >= TestConfig.current.maxRecordedValues)
            recordedAccuracies.RemoveAt(0);
        recordedAccuracies.Add(currentAccuracy);
    }

    private void BeatDetector_beatDetected(BeatDetector obj)
    {
        if (Math.Abs(currentBeat.dspTime - currentDSPTimeSong) <= TestConfig.current.beatBuffer &&
            !currentBeatHit)
        {
            currentBeatHit = true;
            currentAccuracy += TestConfig.current.correctBeatAccuracyBonus;
        }
    }

    void StopTest()
    {
        if (testRoutine != null)
        {
            StopCoroutine(testRoutine);
            testRoutine = null;
        }
        testState = TestState.Idle;
        BeatDetector.beatDetected -= BeatDetector_beatDetected;
    }

    private void StopDataLoading()
    {
        loadingDataPanel.SetActive(false);
        if (loadRoutine != null)
            StopCoroutine(loadRoutine);
        loadRoutine = null;
    }

    void LoadData()
    {
        string data1 = File.ReadAllText(FullMotionTrackingDataCapturer.path + recording1.fileName);
        string data2 = File.ReadAllText(FullMotionTrackingDataCapturer.path + recording2.fileName);
        var reader1 = new FullDataReader();
        var reader2 = new FullDataReader();
        reader1.Read(data1);
        reader2.Read(data2);
        recording1.Init(reader1);
        recording2.Init(reader2);
    }

    public Vector3 GetMarkerPos(MotionRecording recording, int id, int frameID = -1)
    {
        if (frameID >= 0)
        {            
            if (TryGetMoCapFrame(recording, out FullMocapFrame frame, frameID))
            {
                if (frame.idToArrayIndex.ContainsKey(id))
                {
                    int idx = frame.idToArrayIndex[id];
                    return frame.positions[idx];
                }
                Debug.LogWarning("Could not find provided marker ID in recording: " + recording.fileName);
                return default;
            }
        }
        else
        {
            if (TryGetMoCapFrame(recording, out FullMocapFrame frame))
            {
                if (frame.idToArrayIndex.ContainsKey(id))
                {
                    int idx = frame.idToArrayIndex[id];
                    return frame.positions[idx];
                }
                Debug.LogWarning("Could not find provided marker ID in recording: " + recording.fileName);
                return default;
            }
        }
        Debug.LogWarning("Could not get MoCap frame in: " + recording.fileName);
    
        return default;
    }

    public TrackingCondition GetMarkerCond(MotionRecording recording, int id, int frameID = -1)
    {
        if (frameID >= 0)
        {
            if (TryGetMoCapFrame(recording, out FullMocapFrame frame, frameID))
            {
                if (frame.idToArrayIndex.ContainsKey(id))
                {
                    int idx = frame.idToArrayIndex[id];
                    return frame.conditions[idx];
                }
                Debug.LogWarning("Could not find provided marker ID in recording: " + recording.fileName);
            }
        }
        else
        {
            if (TryGetMoCapFrame(recording, out FullMocapFrame frame))
            {
                if (frame.idToArrayIndex.ContainsKey(id))
                {
                    int idx = frame.idToArrayIndex[id];
                    return frame.conditions[idx];
                }
                Debug.LogWarning("Could not find provided marker ID in recording: " + recording.fileName);
            }
        }
        return TrackingCondition.Undefined;
    }

    private bool TryGetMoCapFrame(MotionRecording recording, out FullMocapFrame frame)
    {
        frame = default;
        if (dataState != DataState.Loaded)
        {
            return false;
        }
        
        if (!recording.initialized)
        {
            return false;
        }
        double selectedDspTime = 0d;
        if (recording.currentFrameIndex < recording.dspTimes.Length)
            selectedDspTime = recording.dspTimes[recording.currentFrameIndex];
        else
            return false;
        while (currentDSPTimeMotion - selectedDspTime + recording.dspTimeOffset > 0d)
        {
            recording.currentFrameIndex++;
            if (recording.currentFrameIndex < recording.dspTimes.Length)
                selectedDspTime = recording.dspTimes[recording.currentFrameIndex];
            else
                return false;
        }

        frame = recording.dataReader.data[selectedDspTime];
            
        return true;        
    }

    private bool TryGetMoCapFrame(MotionRecording recording, out FullMocapFrame frame, int frameIndex)
    {
        frame = default;
        if (dataState != DataState.Loaded)
        {
            return false;
        }

        if (!recording.initialized)
        {
            return false;
        }


        double selectedDspTime = recording.dspTimes[frameIndex];
        frame = recording.dataReader.data[selectedDspTime];

        return true;

    }
}

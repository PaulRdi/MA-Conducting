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

    public static event Action dataLoaded;
    public static event Action<double> tick;
    public static event Action<MarkerGroup, Transform> onCalibrate;

    [SerializeField] MotionRecording recording1;
    [SerializeField] MotionRecording recording2;
    [SerializeField] Song song;

    [SerializeField] Transform recordedSuit_rightHandIKRef, recordedSuit_leftHandIKRef;
    [SerializeField] MarkerGroup recordedSuit_rightHand, recordedSuit_leftHand;
    [SerializeField] Transform rigHip;
    [SerializeField] Transform rigParent;
    [SerializeField] MarkerGroup markerHip;
    [SerializeField] Animator rigAnimator;

    [SerializeField] Button tryStartTestButton;
    [SerializeField] GameObject loadingDataPanel;
    [SerializeField] GameObject loadingDataErrorPanel;
    [SerializeField] TextMeshProUGUI loadingDataErrorText;


    public bool manualTick = false;
    bool tickKeyPressed;

    public DataState dataState { get; private set; }
    public TestState testState { get; private set; }
    Coroutine loadRoutine;
    Coroutine testRoutine;

    double startDSPTime;
    double currentDSPTime;
    double lastDSPTime;

    void Awake()
    {
        dataState = DataState.NotLoaded;
        testState = TestState.Idle;
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
    }
    private void LateUpdate()
    {
        lastDSPTime = AudioSettings.dspTime;
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
        testState = TestState.Initializing;
        currentDSPTime = 0.0d;
        startDSPTime = AudioSettings.dspTime;
        TryGetMoCapFrame(recording1, out FullMocapFrame frame0_rec1, 0);
        TryGetMoCapFrame(recording2, out FullMocapFrame frame0_rec2, 0);
        recordedSuit_rightHand.ForceMeasurement(0);
        recordedSuit_leftHand.ForceMeasurement(0);
        markerHip.ForceMeasurement(0);
        rigAnimator.enabled = false;
        Physics.SyncTransforms();
        Util.CalibrateIK(
            recordedSuit_rightHandIKRef,
            recordedSuit_leftHandIKRef,
            recordedSuit_rightHand.controllingTransform,
            recordedSuit_leftHand.controllingTransform,
            rigParent);
        onCalibrate?.Invoke(markerHip, rigHip);
        rigAnimator.enabled = true;
        while (currentDSPTime < 300.0d)
        {
            if (!manualTick)
            {
                currentDSPTime = AudioSettings.dspTime - startDSPTime;
                yield return null;
            }
            else
            {
                currentDSPTime += AudioSettings.dspTime - lastDSPTime;
                yield return new WaitUntil(() => tickKeyPressed);
            }
            if (lastDSPTime != AudioSettings.dspTime)
                tick?.Invoke(AudioSettings.dspTime - lastDSPTime);
            tickKeyPressed = false;
        }
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
        while (currentDSPTime - selectedDspTime + recording.dspTimeOffset > 0d)
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

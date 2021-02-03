using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using PhaseSpace;
using PhaseSpace.Unity;
using System;

public enum DataState
{
    NotLoaded,
    LoadingData,
    Loaded
}
public enum TestState
{
    Off,
    Initializing,
    Running
}
public class TestManagerVersion2 : MonoBehaviour
{

    public static event Action dataLoaded;
    public static event Action<MarkerGroup, Transform> onCalibrate;

    [SerializeField] MotionRecording recording1;
    [SerializeField] MotionRecording recording2;

    [SerializeField] Transform recordesSuit_rightHandIKRef, recordedSuit_leftHandIKRef;
    [SerializeField] Transform recordedSuit_rightHandTransform, recordedSuit_leftHandTransform;
    [SerializeField] Transform rigHip;
    [SerializeField] Transform markerHip;

    public DataState dataState { get; private set; }
    public TestState testState { get; private set; }
    Coroutine loadRoutine;
    Coroutine testRoutine;

    double currentDSPTime;

    void Awake()
    {
        dataState = DataState.NotLoaded;
        testState = TestState.Off;
        TryLoadData();
    }

    void TryStartTest()
    {
        if (dataState != DataState.Loaded)
            return;

    }

    void TryLoadData()
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
            loadRoutine = StartCoroutine(LoadRoutine());
        else
            Debug.LogError("Could not find a recording file. Please assign the correct file names to the config files.");

    }

    IEnumerator TestRoutine()
    {
        testState = TestState.Initializing;
        currentDSPTime = 0.0d;
        TryGetMoCapFrame(recording1, out FullMocapFrame frame0);
        yield return null;
    }

    IEnumerator LoadRoutine()
    {
        dataState = DataState.LoadingData;
        Debug.Log("Data loading: <color=#ffaa00>start</color>");

        Task.Factory.StartNew(() => LoadData())
            .ContinueWith(
                t =>
                {
                    if (t.IsFaulted)
                    {
                        t.Exception.Flatten().Handle(e =>
                        {
                            Debug.Log("Data Loading: <color=#ff0000>error</color> \n" + e.ToString());
                            dataState = DataState.NotLoaded;
                            return true;
                        });
                    }
                    else
                    {
                        dataState = DataState.Loaded;
                    }
                }, 
                TaskScheduler.FromCurrentSynchronizationContext());

        yield return new WaitUntil(() => dataState == DataState.Loaded);
        dataLoaded?.Invoke();
        Debug.Log("Data loading: <color=#00ff00>success</color>");
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

    public Vector3 GetMarkerPos(MotionRecording recording, int id)
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
        return default;
    }

    public TrackingCondition GetMarkerCond(MotionRecording recording, int id)
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
        return default;
    }

    private bool TryGetMoCapFrame(MotionRecording recording, out FullMocapFrame frame)
    {
        MotionRecording rec;
        frame = default;

        if (dataState == DataState.NotLoaded)
        {
            Debug.LogError("Test was not loaded. Cannot get marker data.");
            return false;
        }
        if (recording == recording1)
            rec = recording1;
        else if (recording == recording2)
            rec = recording2;
        else
        {
            Debug.LogError("Error getting mocap frame. An invalid recording was assigned.");
            return false;
        }
        frame = recording.dataReader.data[currentDSPTime];
        return true;
    }
}

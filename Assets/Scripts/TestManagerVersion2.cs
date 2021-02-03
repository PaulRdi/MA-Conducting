using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using PhaseSpace;
using PhaseSpace.Unity;
using System;
public enum TestState
{
    NotLoaded,
    LoadingData,
    Calibrating,
    RunningSong,
    Loaded
}
public class TestManagerVersion2 : MonoBehaviour
{

    public static event Action dataLoaded;

    [SerializeField] MotionRecording recording1;
    [SerializeField] MotionRecording recording2;
    Dictionary <MotionRecording, FullDataReader> motionReaders;

    public TestState state { get; private set; }
    Coroutine loadRoutine;

    double currentDSPTime;

    void Awake()
    {
        state = TestState.NotLoaded;
        TryLoadData();
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

    IEnumerator LoadRoutine()
    {
        state = TestState.LoadingData;
        currentDSPTime = 0.0d;
        motionReaders = new Dictionary<MotionRecording, FullDataReader>();
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
                            state = TestState.NotLoaded;
                            return true;
                        });
                    }
                    else
                    {
                        state = TestState.Loaded;
                    }
                }, 
                TaskScheduler.FromCurrentSynchronizationContext());

        yield return new WaitUntil(() => state == TestState.Loaded);
        dataLoaded?.Invoke();
        Debug.Log("Data loading: <color=#00ff00>success</color>");
    }

    void LoadData()
    {
        string data1 = File.ReadAllText(FullMotionTrackingDataCapturer.path + recording1.fileName);
        string data2 = File.ReadAllText(FullMotionTrackingDataCapturer.path + recording2.fileName);
        motionReaders.Add(recording1, new FullDataReader());
        motionReaders.Add(recording2, new FullDataReader());
        motionReaders[recording1].Read(data1);
        motionReaders[recording2].Read(data2);
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

        if (state == TestState.NotLoaded)
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
        FullDataReader fdr = motionReaders[recording];
        frame = fdr.data[currentDSPTime];
        return true;
    }
}

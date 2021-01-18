using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using PhaseSpace;
using PhaseSpace.Unity;

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

    [SerializeField] MotionRecording recording1;
    [SerializeField] MotionRecording recording2;
    Dictionary <MotionRecording, FullDataReader> motionReaders;

    public TestState state { get; private set; }
    Coroutine testRoutine;

    double currentDSPTime;

    void Awake()
    {
        state = TestState.NotLoaded;
    }

    void TryStartTest()
    {
        if (testRoutine == null &&
            File.Exists(FullMotionTrackingDataCapturer.path + recording1.fileName) &&
            File.Exists(FullMotionTrackingDataCapturer.path + recording2.fileName))
                testRoutine = StartCoroutine(TestRoutine());
    }

    IEnumerator TestRoutine()
    {
        state = TestState.LoadingData;
        currentDSPTime = 0.0d;
        motionReaders = new Dictionary<MotionRecording, FullDataReader>();
        Task.Factory.StartNew(() => LoadData())
            .ContinueWith(t => state = TestState.Loaded);

        yield return new WaitUntil(() => state == TestState.Loaded);
    }

    async void LoadData()
    {
        string data1 = await Task.Run<string>(() => File.ReadAllText(FullMotionTrackingDataCapturer.path + recording1.fileName));
        string data2 = await Task.Run<string>(() => File.ReadAllText(FullMotionTrackingDataCapturer.path + recording2.fileName));
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

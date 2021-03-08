using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PhaseSpace.OWL;
using PhaseSpace.Unity;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Data Layout:
/// 
/// f:DSP_TIME
/// Marker ID | X | Y | Z | Condition
/// Marker ID | X | Y | Z | Condition
/// etc.
/// optional in last place:| m |
/// </summary>
public class FullMotionTrackingDataCapturer : MonoBehaviour
{
    public static string path => Application.streamingAssetsPath + "/FullRecordings/";
    bool recording;
    public bool writing { get; private set; }
    [SerializeField] OWLClient client;
    [SerializeField] string fileName = "full_recording.json";
    List<FullMocapFrame> data;
    double startDSPTime;
    double lastDSPTime;
    bool markNextFrame = false;

    private void Awake()
    {
        recording = false;
        writing = false;
    }

    private void Update()
    {
        Debug.Log(client.State);
        if (client != null &&
            client.State == OWLClient.ConnectionState.Initialized &&
            recording)
        {
            if (lastDSPTime != AudioSettings.dspTime)
            {
                FullMocapFrame frame = new FullMocapFrame(
                        AudioSettings.dspTime - startDSPTime,
                        markNextFrame,
                        client.Markers.Count);

                for (int i = 0; i < client.Markers.Count; i++)
                {
                    var marker = client.Markers[i];
                    frame.idToArrayIndex.Add((int)marker.id, i);
                    frame.positions[i] = marker.position;
                    frame.conditions[i] = marker.Condition;
                }
                markNextFrame = false;
                data.Add(frame);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                markNextFrame = true;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F6))
            {
                TryStopRecroding();
            }
            lastDSPTime = AudioSettings.dspTime;
        }
        else
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
            {
                TryStartRecording();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.F7))
            {
                Serialize();
            }
        }
    }

    private void TryStopRecroding()
    {
        Debug.Log("stopped recording. \n" + "Frame Count = " + data.Count);
        recording = false;
    }

    bool TryStartRecording()
    {
        if (client.State == OWLClient.ConnectionState.Initialized)
        {
            markNextFrame = false;
            data = new List<FullMocapFrame>();
            recording = true;
            startDSPTime = AudioSettings.dspTime;
            Debug.Log("Starting recording!");
            return true;
        }
        else
        {
            Debug.Log("Could not start recording");
            return false;
        }
    }

    void Serialize()
    {
        if (recording)
        {
            Debug.LogWarning("Cant save data while recording.");
            return;
        }
        if (writing)
        {
            Debug.LogWarning("Cant save data while writing data.");
            return;
        }
        if (data == null)
        {
            Debug.LogWarning("No recording data.");
            return;
        }
        Debug.Log("Start string building");
        
        Debug.Log("Start write");
        writing = true;
        Task.Factory.StartNew(() =>
            {
                File.WriteAllText(path+fileName, JsonConvert.SerializeObject(data));              
                writing = false;
            })
            .ContinueWith(
            (t) =>
            {                
                Debug.Log("finished write");
            }, 
            TaskScheduler.FromCurrentSynchronizationContext());
    }
}


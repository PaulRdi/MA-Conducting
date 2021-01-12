using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PhaseSpace.OWL;
using PhaseSpace.Unity;
using System.IO;

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
    bool recording;
    [SerializeField] OWLClient client;
    [SerializeField] int[] trackingMarkers;
    [SerializeField] string fileName = "full_recording.csv";
    List<string> data;
    double startDSPTime;

    private void Awake()
    {
        recording = false;
    }

    private void Update()
    {
        if (client != null &&
            client.State == OWLClient.ConnectionState.Streaming &&
            recording)
        {
            data.Add("f:" + (AudioSettings.dspTime - startDSPTime).ToString() + ",");
            foreach (PhaseSpace.Unity.Marker m in client.Markers)
            {
                data.Add(m.id.ToString() + "," 
                    + m.position.x.ToString() + "," 
                    + m.position.y.ToString() + ","
                    + m.position.z.ToString() + ","
                    + (int)m.Condition);
                
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                data.Add("m,");
            }
        }
    }

    bool TryStartRecording()
    {
        if (client.State == OWLClient.ConnectionState.Streaming)
        {
            data = new List<string>();
            recording = true;
            startDSPTime = AudioSettings.dspTime;
            return true;
        }
        else
        {
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
        if (data == null)
        {
            Debug.LogWarning("No recording data.");
            return;
        }
        string dump = "";
        foreach(string s in data)
        {
            dump += s;
            dump += "\n";
        }
        File.WriteAllText(Application.streamingAssetsPath + "/FullRecordings/" + fileName, dump);
    }
}


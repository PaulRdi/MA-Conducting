using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using PhaseSpace.Unity;


public class FullDataReaderTestBehaviour : MonoBehaviour
{
    FullDataReader fdr;
    [SerializeField] string file = "full_recording.csv";
    List<Vector3> gizmosToDraw;
    bool reading;
    private void Awake()
    {
        reading = false;
    }
    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.F8))
        {
            DeserializeSync();
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log(fdr.data[0].positions[0]);
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.F10) &&
            fdr != null &&
            !reading)
        {
            StartCoroutine(ReplayCapture());
        }
    }
    IEnumerator ReplayCapture()
    {
        double startDSPTime = AudioSettings.dspTime;
        double relativeDSPTime = 0.0d;
        double targetDSPTime = fdr.data.Keys.Last();
        FullMocapFrame currentFrame = fdr.data[fdr.data.Keys.First()];
        IEnumerator<double> dspTimes = fdr.data.Keys.GetEnumerator();
        
        while (relativeDSPTime <= targetDSPTime)
        {
            if (relativeDSPTime >= dspTimes.Current)
            {
                if (!dspTimes.MoveNext())
                    break;

            }
            currentFrame = fdr.data[dspTimes.Current];
            gizmosToDraw = new List<Vector3>();
            foreach(int id in currentFrame.idToArrayIndex.Keys)
            {
                
                int index = currentFrame.idToArrayIndex[id];
                Vector3 pos = currentFrame.positions[index];

                if (currentFrame.conditions[index] >= TrackingCondition.Poor)
                {
                    gizmosToDraw.Add(pos);
                }
            }
                
            yield return null;
            relativeDSPTime = AudioSettings.dspTime - startDSPTime;
        }
        

    }
    private void OnDrawGizmos()
    {
        if (gizmosToDraw == null)
            return;

        foreach (Vector3 pos in gizmosToDraw)
            Gizmos.DrawSphere(pos, .1f);
    }
    private void DeserializeAsync()
    {
        string filePath = FullMotionTrackingDataCapturer.path + file;
        if (File.Exists(filePath))
        {
            Debug.Log("Starting Deserialize");
            this.reading = true;
            Task.Factory.StartNew<string>(() =>
            {
                return File.ReadAllText(filePath);
            })
            .ContinueWith(t =>
            {
                this.fdr = new FullDataReader();
                this.fdr.Read(t.Result);
            })
            .ContinueWith(t =>
            {
                FinishDeserialize();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    private void FinishDeserialize()
    {
        Debug.Log("Deserialize complete");
        this.reading = false;
    }

    private void DeserializeSync()
    {
        string filePath = FullMotionTrackingDataCapturer.path + file;
        if (File.Exists(filePath))
        {
            Debug.Log("Starting Deserialize");
            this.reading = true;
            string fileText = File.ReadAllText(filePath);            
            this.fdr = new FullDataReader();
            this.fdr.Read(fileText);
            Debug.Log("Deserialize complete");
            this.reading = false;

        }
    }
}
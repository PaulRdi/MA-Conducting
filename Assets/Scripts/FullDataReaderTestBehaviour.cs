using System;
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
    bool reading;
    private void Awake()
    {
        reading = false;
    }
    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.F8))
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
                    Debug.Log("Deserialize complete");
                    this.reading = false;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log(fdr.data[0].positions[0]);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.UI;
using System;
using System.Runtime.Remoting.Messaging;

public class MocapDataStreamer : MonoBehaviour
{
    [SerializeField] string fileName = "lohengrin_test.json";
    [SerializeField] Button testLoadButton, testPlayButton;
    [SerializeField]
    [Range(0f, 1f)]
    float debugGizmoSize = .03f;
    Transform[] debugTransforms;
    string folderPath;
    MocapDataStream dataStream;
    Coroutine streamingRoutine;
    void Start()
    {
        folderPath = Application.streamingAssetsPath;
    }
    private void OnEnable()
    {
        testLoadButton.onClick.AddListener(TestLoadButtonClicked);
        testPlayButton.onClick.AddListener(TestPlay);
    }

   

    private void OnDisable()
    {
        testLoadButton.onClick.RemoveListener(TestLoadButtonClicked);
        testPlayButton.onClick.RemoveListener(TestPlay);


    }
    private void TestPlay()
    {
        if (dataStream == null)
        {
            Debug.LogError("Had no data stream. Make sure to load first", this);
            return;
        }
        if (streamingRoutine != null)
        {
            Debug.LogError("Data Stream already running. Cannot start twice", this);
            return;
        }
        debugTransforms = new Transform[dataStream.data.numMarkerGroups];
        for (int i = 0; i < dataStream.data.numMarkerGroups; i++)
        {
            debugTransforms[i] = new GameObject("data stream obj_" + i.ToString()).transform;
        }
        streamingRoutine = StartCoroutine(dataStream.Play(this, debugTransforms));   
    }

    

    private void TestLoadButtonClicked()
    {
        StartAndLoadStream();
    }

    public void StartAndLoadStream()
    {
        Task.Run(LoadStream)
            .ContinueWith(StartStream, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public async Task<string> LoadStream()
    {
        return await Task.Run<string>(() => File.ReadAllText(folderPath + "/" + fileName));
    }
    public object StartStream(Task<string> data)
    {
        this.dataStream = new MocapDataStream(data.Result);
        return data;
    }

    private void OnDrawGizmos()
    {
        if (debugTransforms == null)
            return;

        Gizmos.color = Color.magenta;
        foreach (Transform t in debugTransforms)
            Gizmos.DrawSphere(t.position, debugGizmoSize);
    }
}



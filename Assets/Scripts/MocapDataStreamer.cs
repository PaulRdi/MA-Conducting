using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.UI;
using System;
using System.Runtime.Remoting.Messaging;
using TMPro;
public class MocapDataStreamer : MonoBehaviour
{
    [SerializeField] string fileName = "lohengrin_test.json";
    [SerializeField] Button testLoadButton, testPlayButton;
    [SerializeField] TextMeshProUGUI dspText;

    [SerializeField]
    [Range(0f, 1f)]
    float debugGizmoSize = .03f;
    Transform[] debugTransforms;
    string folderPath;
    public MocapDataStream dataStream { get; private set; }
    Coroutine streamingRoutine;
    internal bool initialized;

    void Start()
    {
        folderPath = Application.streamingAssetsPath;
    }

    private void Update()
    {
        if (dspText != null && dataStream != null)
            dspText.text = dataStream.currDSP.ToString("0.00");
    }

    private void OnEnable()
    {
        testLoadButton?.onClick.AddListener(TestLoadButtonClicked);
        testPlayButton?.onClick.AddListener(TestPlay);
    }

   

    private void OnDisable()
    {
        testLoadButton?.onClick.RemoveListener(TestLoadButtonClicked);
        testPlayButton?.onClick.RemoveListener(TestPlay);


    }

    public void Play(Transform[] controllingTransforms)
    {
        StartCoroutine(dataStream.Play(controllingTransforms));
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
        streamingRoutine = StartCoroutine(dataStream.Play(debugTransforms));   
    }

    

    private void TestLoadButtonClicked()
    {
        LoadStreamAndInit();
    }

    public void LoadStreamAndInit()
    {
        initialized = false;
        Task.Run(LoadStream)
            .ContinueWith(InitStreamData, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public async Task<string> LoadStream()
    {
        initialized = false;
        return await Task.Run<string>(() => File.ReadAllText(folderPath + "/" + fileName));
    }
    public object InitStreamData(Task<string> data)
    {
        this.dataStream = new MocapDataStream(data.Result);
        initialized = true;
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



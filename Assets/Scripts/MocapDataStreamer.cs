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
    [SerializeField] Button testNextFrameButton, testPrevFrameButton;
    [SerializeField] TextMeshProUGUI dspText;
    [SerializeField] TextMeshProUGUI frameNumberText;

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

        if (frameNumberText != null && dataStream != null)
            frameNumberText.text = dataStream.currentFrame.ToString();
    }

    private void OnEnable()
    {
        testLoadButton?.onClick.AddListener(TestLoadButtonClicked);
        testPlayButton?.onClick.AddListener(TestPlay);
        testNextFrameButton?.onClick.AddListener(TestNextFrameButtonClicked);
        testPrevFrameButton?.onClick.AddListener(TestPrevFrameButtonClicked);
    }

   

    private void OnDisable()
    {
        testLoadButton?.onClick.RemoveListener(TestLoadButtonClicked);
        testPlayButton?.onClick.RemoveListener(TestPlay);
        testNextFrameButton?.onClick.RemoveListener(TestNextFrameButtonClicked);
        testPrevFrameButton?.onClick.RemoveListener(TestPrevFrameButtonClicked);

    }

    void TestNextFrameButtonClicked()
    {
        if (!initialized)
            return;
        if (streamingRoutine != null)
            StopStreaming();

        int nframes = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            nframes = 10;
        if (dataStream.currentFrame + nframes < dataStream.data.dspTimeToMoCapFrame.Count)
            dataStream.currentFrame += nframes;
        else
            dataStream.currentFrame = 0;

        dataStream.UpdateControllingTransforms(debugTransforms);
    }

    void TestPrevFrameButtonClicked()
    {
        if (!initialized)
            return;
        if (streamingRoutine != null)        
            StopStreaming();
        int nframes = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            nframes = 10;
        if (dataStream.currentFrame - nframes >= 0)
            dataStream.currentFrame -= nframes;
        else
            dataStream.currentFrame = dataStream.data.dspTimeToMoCapFrame.Count - 1;

        dataStream.UpdateControllingTransforms(debugTransforms);
    }

    private void StopStreaming()
    {
        StopCoroutine(streamingRoutine);
        streamingRoutine = null;
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

    private async Task<string> LoadStream()
    {
        
        initialized = false;
        return await Task.Run<string>(() => File.ReadAllText(folderPath + "/" + fileName));
    }
    public object InitStreamData(Task<string> data)
    {
        this.dataStream = new MocapDataStream(data.Result);
        debugTransforms = new Transform[dataStream.data.numMarkerGroups];
        for (int i = 0; i < dataStream.data.numMarkerGroups; i++)
        {
            debugTransforms[i] = new GameObject("data stream obj_" + i.ToString()).transform;
        }
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



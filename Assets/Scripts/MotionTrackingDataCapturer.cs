using PhaseSpace.OWL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.IO;

public class MotionTrackingDataCapturer : MonoBehaviour
{
    public static event Action<MarkerGroup, Transform> onCalibrate; //pushes hip transforms (marker hip, then rig hip)

    [SerializeField] Song song;
    [SerializeField] MarkerGroup[] markerGroups;
    [SerializeField] Transform skeletonRoot;
    [SerializeField] Button startCapturingButton, stopCapturingButton;
    [SerializeField] Transform rigHip;
    [SerializeField] MarkerGroup markerHip;

    [SerializeField]
    Transform
        rightHandIKRef,
        leftHandIKRef,
        rightHandSuitTransform,
        leftHandSuitTransform,
        rig;
    double startDSPTime;
    bool capturing;

    private void OnEnable()
    {
        capturing = false;
        startCapturingButton.onClick.AddListener(StartCapture);
        stopCapturingButton.onClick.AddListener(StopCapture);
    }
    private void OnDisable()
    {
        startCapturingButton.onClick.RemoveListener(StartCapture);
        stopCapturingButton.onClick.RemoveListener(StopCapture);
    }

    private void Update()
    {
        if (capturing)
        {
            song.moCapData.RecordFrame(
                markerGroups,
                AudioSettings.dspTime - startDSPTime);

            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                song.moCapData.SetFirstBeatRelativeDSPTime(AudioSettings.dspTime - startDSPTime);
            }
        }
    }

    void StartCapture()
    {
        if (capturing)
        {
            Debug.LogWarning("cannot capture if is already capturing");
            return;
        }

        Util.CalibrateIK(
            rightHandIKRef,
            leftHandIKRef,
            rightHandSuitTransform,
            leftHandSuitTransform,
            rig);
        onCalibrate?.Invoke(markerHip, rigHip);
        capturing = true;
        startDSPTime = AudioSettings.dspTime;
        song.moCapData = new MoCapData(skeletonRoot.position, skeletonRoot.rotation);

    }

    void StopCapture()
    {
        if (!capturing)
            return;
        capturing = false;

        string baseFileName = "lohengrin_test.json";
        string fileName = baseFileName;
        int counter = 0;
        while (File.Exists(Application.streamingAssetsPath +"/" + baseFileName))
        {
            fileName = baseFileName + "_" + counter.ToString();
            counter++;
        }
        song.moCapData.Save(fileName);
    }
}

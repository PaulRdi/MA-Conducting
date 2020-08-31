using PhaseSpace.OWL;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTracker : MonoBehaviour
{
    [SerializeField] MarkerGroup referencedMarkerGroup;
    [SerializeField] bool useOffsetToHip = true;
    [SerializeField] MarkerGroup markerHip;
    Transform rigHip;
    bool calibrated;
    Vector3 calibratedOffset;
    // Start is called before the first frame update
    void OnEnable()
    {
        TestManager.onCalibrate += TestManager_onCalibrate;
        MotionTrackingDataCapturer.onCalibrate += MotionTrackingDataCapturer_onCalibrate;
        calibrated = false;
    }

    private void MotionTrackingDataCapturer_onCalibrate(MarkerGroup arg1, Transform arg2)
    {
        Init(arg1, arg2);
    }

    void OnDisable()
    {
        TestManager.onCalibrate -= TestManager_onCalibrate;
        MotionTrackingDataCapturer.onCalibrate -= MotionTrackingDataCapturer_onCalibrate;

    }

    private void TestManager_onCalibrate(MarkerGroup markerHip, Transform rigHip)
    {
        Init(markerHip, rigHip);
    }

    private void Init(MarkerGroup markerHip, Transform rigHip)
    {
        calibratedOffset = transform.position - referencedMarkerGroup.lastAveragePosition;
        this.markerHip = markerHip;
        this.rigHip = rigHip;
        calibrated = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (calibrated)
            UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (useOffsetToHip)
        {
            Vector3 markerGroupOffsetFromHip = referencedMarkerGroup.lastAveragePosition - markerHip.lastAveragePosition;
            transform.position = rigHip.position + markerGroupOffsetFromHip;
        }
        else
        {
            transform.position = referencedMarkerGroup.lastAveragePosition + calibratedOffset;
        }
    }
}

using PhaseSpace.OWL;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTracker : MonoBehaviour
{
    [SerializeField] Transform referencedMarkerGroup;
    [SerializeField] bool useOffsetToHip = true;
    [SerializeField] Transform markerHip;

    Transform rigHip;
    bool calibrated;
    Vector3 calibratedOffset;
    // Start is called before the first frame update
    void OnEnable()
    {
        TestManager.onCalibrate += TestManager_onCalibrate;
        TestManagerVersion2.onCalibrate += TestManagerVersion2_onCalibrate;
        MotionTrackingDataCapturer.onCalibrate += MotionTrackingDataCapturer_onCalibrate;
        calibrated = false;
    }
    void OnDisable()
    {
        TestManager.onCalibrate -= TestManager_onCalibrate;
        TestManagerVersion2.onCalibrate -= TestManagerVersion2_onCalibrate;
        MotionTrackingDataCapturer.onCalibrate -= MotionTrackingDataCapturer_onCalibrate;
    }

    private void TestManagerVersion2_onCalibrate(MarkerGroup arg1, Transform arg2)
    {
        Init(arg1.transform, arg2);
    }

    private void MotionTrackingDataCapturer_onCalibrate(MarkerGroup arg1, Transform arg2)
    {
        Init(arg1.transform, arg2);
    }

    

    private void TestManager_onCalibrate(Transform markerHip, Transform rigHip)
    {
        Init(markerHip, rigHip);
    }

    private void Init(Transform markerHip, Transform rigHip)
    {
        calibratedOffset = transform.position - referencedMarkerGroup.position;
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
            Vector3 markerGroupOffsetFromHip = referencedMarkerGroup.position - markerHip.position;
            transform.position = rigHip.position + markerGroupOffsetFromHip;
        }
        else
        {
            transform.position = referencedMarkerGroup.position + calibratedOffset;
        }
    }
}

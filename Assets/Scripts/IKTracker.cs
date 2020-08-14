using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTracker : MonoBehaviour
{
    [SerializeField] MarkerGroup referencedMarkerGroup;
    [SerializeField] bool useOffsetToHip = true;
    Transform markerHip, rigHip;
    bool calibrated;
    Vector3 calibratedOffset;
    // Start is called before the first frame update
    void OnEnable()
    {
        TestManager.onCalibrate += TestManager_onCalibrate;
        calibrated = false;
    }
    void OnDisable()
    {
        TestManager.onCalibrate -= TestManager_onCalibrate;
    }

    private void TestManager_onCalibrate(Transform markerHip, Transform rigHip)
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
            Vector3 markerGroupOffsetFromHip = referencedMarkerGroup.lastAveragePosition - markerHip.position;
            transform.position = rigHip.position + markerGroupOffsetFromHip;
        }
        else
        {
            transform.position = referencedMarkerGroup.lastAveragePosition + calibratedOffset;
        }
    }
}

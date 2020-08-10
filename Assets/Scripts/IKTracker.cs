using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTracker : MonoBehaviour
{
    [SerializeField] MarkerGroup referencedMarkerGroup;
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

    private void TestManager_onCalibrate(Transform hip)
    {
        calibratedOffset = transform.position - referencedMarkerGroup.lastAveragePosition;
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
        transform.position = referencedMarkerGroup.lastAveragePosition + calibratedOffset;
    }
}

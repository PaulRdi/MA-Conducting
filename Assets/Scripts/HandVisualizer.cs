using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandVisualizer : MonoBehaviour
{
    Vector3 calibratedOffset;
    [SerializeField] Transform referencedMarkerGroup;
    [SerializeField] bool useOffsetToHip;
    Transform markerHip;
    Transform rigHip;
    bool calibrated;
    // Start is called before the first frame update

    private void Awake()
    {
        calibrated = false;
    }
    void OnEnable()
    {
        TestManagerVersion2.realtime_onCalibrate += TestManagerVersion2_realtime_onCalibrate;
    }
    private void OnDisable()
    {
        TestManagerVersion2.realtime_onCalibrate -= TestManagerVersion2_realtime_onCalibrate;
    }

    private void TestManagerVersion2_realtime_onCalibrate(MarkerGroup arg1, Transform arg2)
    {
        Init(arg1.controllingTransform, arg2);
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

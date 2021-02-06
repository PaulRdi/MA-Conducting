using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerCustom : MonoBehaviour
{
    public int id;
    public DataSource source => group.dataSource;
    public MotionRecording recording => group.recording;
    MarkerGroup group;
    public void Init(MarkerGroup group)
    {
        this.group = group;
    }

    private void OnEnable()
    {
        //TestManagerVersion2.tick += Tick;
    }

    private void OnDisable()
    {
        //TestManagerVersion2.tick -= Tick;
    }
    void Update()
    {
        transform.position = DataRouter.MPos(source, id, recording);
    }
    void Tick(double dt)
    {
        transform.position = DataRouter.MPos(source, id, recording);
    }

    internal void ForceMeasurement(int frameID)
    {
        transform.position = DataRouter.MPos(source, id, recording, frameID);
    }
}

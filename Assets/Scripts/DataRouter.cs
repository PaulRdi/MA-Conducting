using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PhaseSpace;
using PhaseSpace.Unity;
public class DataRouter : MonoBehaviour
{
    static DataRouter instance;
    public OWLClient owl;
    public TestManagerVersion2 tm;
    void Awake()
    {
        instance = this;
        tm = FindObjectOfType<TestManagerVersion2>();
        owl = FindObjectOfType<OWLClient>();
    }

    public static Vector3 MPos(DataSource source, int id, MotionRecording r = default)
    {
        switch (source)
        {
            case DataSource.PhaseSpace:
                break;
            case DataSource.Self:
                if (r == default)
                    throw new System.Exception("Must provide a recording to get position from non-phasespace-streaming.");
                return instance.tm.GetMarkerPos(r, id);
                
        }

        return default;
    }

    public static TrackingCondition MCond(DataSource source, int id, MotionRecording r = default)
    {
        switch (source)
        {
            case DataSource.PhaseSpace:
                break;
            case DataSource.Self:
                if (r == default)
                    throw new System.Exception("Must provide a recording to get condition from non-phasespace-streaming.");         
                return instance.tm.GetMarkerCond(r, id);
        }

        return default;
    }
}

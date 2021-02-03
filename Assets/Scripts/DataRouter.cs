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
    public bool owlInitialized
    {
        get
        {
            if (owl?.State == OWLClient.ConnectionState.Initialized ||
                owl?.State == OWLClient.ConnectionState.Open ||
                owl?.State == OWLClient.ConnectionState.Streaming)
                return true;
            return false;
        }
    }
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
                if (instance.owl == default ||
                    !instance.owlInitialized)
                {
                    Debug.LogError("Cannot get OWL based marker position from uninitialized system.");
                    break;
                }
                return instance.owl.Markers[id].position;
            case DataSource.Self:
                if (r == default)
                {
                    Debug.LogError("Must provide a recording to get position from non-phasespace-streaming.");
                    break;
                }
                return instance.tm.GetMarkerPos(r, id);
                
        }

        return default;
    }

    public static TrackingCondition MCond(DataSource source, int id, MotionRecording r = default)
    {
        switch (source)
        {
            case DataSource.PhaseSpace:
                if (instance.owl == default ||
                    !instance.owlInitialized)
                {
                    Debug.LogError("Cannot get OWL based marker position from uninitialized system.");
                    break;
                }
                return instance.owl.Markers[id].Condition;
            case DataSource.Self:
                if (r == default)
                {
                    Debug.LogError("Must provide a recording to get condition from non-phasespace-streaming.");
                    break;
                }
                return instance.tm.GetMarkerCond(r, id);
        }

        return default;
    }
}

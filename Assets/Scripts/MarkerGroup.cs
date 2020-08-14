using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhaseSpace.OWL;
using PhaseSpace.Unity;
using Unity.Mathematics;
using System;
using UnityEngine.Rendering.PostProcessing;

//Dont use rigidbody because e.g. hands are not rigid.
//Average positions of marker to control transform.
//Use HMM to estimate marker position

public class MarkerGroup : MonoBehaviour
{
    [SerializeField] Transform controllingTransform;
    OWLMarker[] markers;
    OWLClient owl;
    public Vector3 lastAveragePosition;
    public int activeMarkerCount;
    Vector3[] headings;
    Vector3[] positions;
    float[] alphaSum;
    bool[] active;
    float alpha = 0.95f;
    float epsilon = 0.01f;
    private float maxExtrapolationDistance = .08f;

    // Start is called before the first frame update
    void Start()
    {
        markers = GetComponentsInChildren<OWLMarker>();
       
        if (markers.Length <= 0)
            Debug.LogWarning("No markers detected for " + this.gameObject.name, this);
        else
        {
            owl = markers[0].owl;
            headings = new Vector3[markers.Length];
            positions = new Vector3[markers.Length];
            alphaSum = new float[markers.Length];
            active = new bool[markers.Length];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (markers.Length <= 0 ||
            !owl.Ready)
            return;

        UpdateMarkerPositions();
        controllingTransform.position = lastAveragePosition;
    }

    private void UpdateMarkerPositions()
    {
        activeMarkerCount = 0;
        Vector3 lastAveragePositionCache = lastAveragePosition;
        lastAveragePosition = Vector3.zero;
        for (int i = 0; i < markers.Length; i++)
        {
            //movement model
            //estimate marker positions using hidden markov model. Alpha is reset once marker is found again
            //transition model is last marker heading
            //gets slower the more uncertain position is
            int id = markers[i].id;
            if (owl.Markers[id].Condition == TrackingCondition.Invalid ||
                owl.Markers[id].Condition == TrackingCondition.Undefined)
            {
                alphaSum[i] *= alpha;
                positions[i] += Vector3.ClampMagnitude(headings[i] * alphaSum[i], maxExtrapolationDistance);
                active[i] = alphaSum[i] > epsilon ? true : false;
            }
            else
            {
                alphaSum[i] = 1.0f;
                active[i] = true;
                headings[i] = markers[i].transform.position - positions[i];
                positions[i] = markers[i].transform.position;
            }

            if (active[i])
            {
                activeMarkerCount++;
                lastAveragePosition += positions[i];
            }
        }
        if (activeMarkerCount > 0)
            lastAveragePosition /= (float)activeMarkerCount;
        else
            lastAveragePosition = lastAveragePositionCache;
        
    }
}

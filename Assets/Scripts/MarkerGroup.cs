using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhaseSpace.OWL;
using PhaseSpace.Unity;
using Unity.Mathematics;
using System;
using UnityEngine.Rendering.PostProcessing;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine.Animations.Rigging;
//Dont use rigidbody because e.g. hands are not rigid.
//Average positions of marker to control transform.
//Use HMM to estimate marker position

public class MarkerGroup : MonoBehaviour
{
    [SerializeField] Transform controllingTransform;
    OWLMarker[] markers;
    OWLClient owl;
    public UnityEngine.Vector3 lastAveragePosition;
    public int activeMarkerCount;
    UnityEngine.Vector3[] headings;
    UnityEngine.Vector3[] positions;
    float[] alphaSum;
    bool[] active;
    [SerializeField] [Range(0f, 1f)] float debugSphereSize = 0.2f;
    [SerializeField] [Range(0f, 1f)] float alpha = 0.95f;
    float epsilon = 0.01f;
    UnityEngine.Vector3 avgHeading;
    //speed the hmm extrapolates with when marker data is lost.
    [SerializeField] float extrapolationSpeed = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        markers = GetComponentsInChildren<OWLMarker>();
       
        if (markers.Length <= 0)
            Debug.LogWarning("No markers detected for " + this.gameObject.name, this);
        else
        {
            owl = markers[0].owl;
            headings = new UnityEngine.Vector3[markers.Length];
            positions = new UnityEngine.Vector3[markers.Length];
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

        UpdateMarkerPositionsMarkov();
        controllingTransform.position = lastAveragePosition;
    }

    private void UpdateMarkerPositionsMarkov()
    {
        activeMarkerCount = 0;
        UnityEngine.Vector3 lastAveragePositionCache = lastAveragePosition;
        lastAveragePosition = UnityEngine.Vector3.zero;
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
                positions[i] += headings[i] * Time.deltaTime;
                //positions[i] += headings[i].normalized * alphaSum[i] * extrapolationSpeed * Time.deltaTime;
                active[i] = alphaSum[i] > epsilon ? true : false;
                headings[i] = UnityEngine.Vector3.Lerp(headings[i], avgHeading, 0.1f);
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
                UnityEngine.Vector3 avgPositionCache = lastAveragePosition;
                lastAveragePosition += positions[i] * alphaSum[i];              
                avgHeading += headings[i];
            }
        }
        if (activeMarkerCount > 0)
            lastAveragePosition /= alphaSum.Sum();
        else
            lastAveragePosition = lastAveragePositionCache;

        avgHeading = avgHeading.normalized;
    }

    private void UpdateMarkerPositionsKalman()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(controllingTransform.position, debugSphereSize);
    }
}

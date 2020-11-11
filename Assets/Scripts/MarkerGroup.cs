using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhaseSpace.OWL;
using PhaseSpace.Unity;
using Unity.Mathematics;
using System;
using UnityEngine.Rendering.PostProcessing;
using System.Linq;
using UnityEngine.Animations.Rigging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
//Dont use rigidbody because e.g. hands are not rigid.
//Average positions of marker to control transform.
//Use HMM to estimate marker position

public class MarkerGroup : MonoBehaviour
{
    [SerializeField] Transform controllingTransform;
    OWLMarker[] markers;
    KalmanFilter[] kalmans;
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
            kalmans = new KalmanFilter[markers.Length];

            int statevecSize = 3;
            for (int i = 0; i < kalmans.Length; i++)
            {
                kalmans[i] = new KalmanFilter(3, 3, 0, Emgu.CV.CvEnum.DepthType.Cv32F);
                Matrix<float> transitionMatrix = new Matrix<float>(statevecSize, statevecSize);
                InitTransitionMatrix(statevecSize, 1, 3, transitionMatrix);

                Matrix<float> measurementMatrix = new Matrix<float>(3, statevecSize);
                measurementMatrix.SetIdentity();

                Matrix<float> processNoise = new Matrix<float>(statevecSize, statevecSize);
                for (int j = 0; j < statevecSize; j++)
                {
                    processNoise[j, j] = 1e-4f;

                }

                Matrix<float> measurementNoise = new Matrix<float>(3, 3);
                for (int j = 0; j < 3; j++)
                {
                    measurementNoise[j, j] = 1e-4f;
                }

                Matrix<float> state = new Matrix<float>(statevecSize, 1);
                state.SetRandNormal(new MCvScalar(0.0f), new MCvScalar(5.0f));

                transitionMatrix.Mat.CopyTo(kalmans[i].TransitionMatrix);
                measurementMatrix.Mat.CopyTo(kalmans[i].MeasurementMatrix);
                measurementNoise.Mat.CopyTo(kalmans[i].MeasurementNoiseCov);
                state.Mat.CopyTo(kalmans[i].StatePre);
                processNoise.Mat.CopyTo(kalmans[i].ErrorCovPre);

            }
        }
    }
    private void InitTransitionMatrix(int statevecSize, int numMarkers, int dim, Matrix<float> transitionMatrix)
    {
        for (int i = 0; i < statevecSize; i++)
        {
            int tmpIndex = i / (numMarkers * dim);

            switch (tmpIndex)
            {
                case 0:
                    transitionMatrix[i, i] = 1 + .1f;
                    break;
                case 1:
                    transitionMatrix[i, i] = -.1f;
                    break;
                default:
                    transitionMatrix[i, i] = 0f;
                    break;
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (markers.Length <= 0 ||
            !owl.Ready)
            return;

        UpdateMarkerPositionsKalman();
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
        for (int i = 0; i < markers.Length; i++)
        {
            kalmans[i].Predict();
            kalmans[i].Correct(
                new Matrix<float>(new float[]{
                    markers[i].transform.position.x,
                    markers[i].transform.position.y,
                    markers[i].transform.position.z }).Mat);
        }
        Matrix<float> sum = new Matrix<float>(3, 1);
        int count = 0;
        foreach (var matrix in kalmans.Select(k => k.StatePost))
        {
            Matrix<float> vals = new Matrix<float>(3,1);
            matrix.ConvertTo(vals, Emgu.CV.CvEnum.DepthType.Cv32F);
            sum += vals;
            count++;
        }

        sum /= (float)count;
        lastAveragePosition = new Vector3(sum[0, 0], sum[1, 0], sum[2, 0]);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(controllingTransform.position, debugSphereSize);
    }
}

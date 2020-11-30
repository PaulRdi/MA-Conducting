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

public enum MarkerPredictionMode
{
    None,
    HMM,
    Kalman,
    KalmanGlobal
}

//Dont use rigidbody because e.g. hands are not rigid.
//Average positions of marker to control transform.
//Use HMM or KF to estimate marker position
public class MarkerGroup : MonoBehaviour
{
    [SerializeField] Transform controllingTransform;
    [SerializeField] MarkerPredictionMode predictionMode = MarkerPredictionMode.Kalman;
    OWLMarker[] markers;
    KalmanFilter[] kalmans;
    KalmanFilter globalKalman;
    OWLClient owl;
    public UnityEngine.Vector3 lastAveragePosition;
    public int activeMarkerCount;
    UnityEngine.Vector3[] headings;
    UnityEngine.Vector3[] positions;
    float[] alphaSum;
    bool[] active;
    [SerializeField] [Range(0f, 1f)] float debugSphereSize = 0.2f;
    [SerializeField] [Range(0f, 1f)] float alpha = 0.95f;
    [SerializeField] int numSavedFrames = 50;
    [SerializeField] float lineWidth = .03f;
    [SerializeField] float measurementNoiseMagnitude = 1e-4f;
    float epsilon = 0.01f;
    UnityEngine.Vector3 avgHeading;
    //speed the hmm extrapolates with when marker data is lost.
    [SerializeField] float extrapolationSpeed = 1.0f;
    // Start is called before the first frame update
    int statevecSize = 6;
    int dim = 3;
    LineRenderer lr;
    Queue<Vector3> lastPositions;

    void Start()
    {
        markers = GetComponentsInChildren<OWLMarker>();
        if (TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
        {
            lr = lineRenderer;
        }
        else
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.widthMultiplier = lineWidth;
            lr.positionCount = numSavedFrames;
        }
        lastPositions = new Queue<Vector3>();
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

            CreateLocalKalman();
            CreateGlobalKalman();
        }
    }

    private void CreateGlobalKalman()
    {
        globalKalman = new KalmanFilter(statevecSize, dim, 0, Emgu.CV.CvEnum.DepthType.Cv32F);
        //transitionMatrix.Mat.CopyTo(globalKalman.TransitionMatrix);
        Matrix<float> measurementMatrix = new Matrix<float>(dim, statevecSize);
        measurementMatrix.SetIdentity();
        //Debug.Log("Measurement Matrix \n" + StringifyMatrix(measurementMatrix));

        Matrix<float> processNoise = new Matrix<float>(statevecSize, statevecSize);
        for (int j = 0; j < statevecSize; j++)
        {
            processNoise[j, j] = 1.0e-4f;
        }

        Matrix<float> measurementNoise = new Matrix<float>(dim, dim);
        for (int j = 0; j < dim; j++)
        {
            measurementNoise[j, j] = 1.0e-3f;
        }

        Matrix<float> state = new Matrix<float>(statevecSize, 1);
        state.SetRandNormal(new MCvScalar(0.0f), new MCvScalar(5.0f));

        Matrix<float> errorCov = new Matrix<float>(statevecSize, statevecSize);
        errorCov.SetIdentity();

        measurementMatrix.Mat.CopyTo(globalKalman.MeasurementMatrix);
        measurementNoise.Mat.CopyTo(globalKalman.MeasurementNoiseCov);
        state.Mat.CopyTo(globalKalman.StatePre);
        processNoise.Mat.CopyTo(globalKalman.ProcessNoiseCov);
        errorCov.Mat.CopyTo(globalKalman.ErrorCovPost);
    }

    private void CreateLocalKalman()
    {
        for (int i = 0; i < kalmans.Length; i++)
        {
            kalmans[i] = new KalmanFilter(statevecSize, dim, 0, Emgu.CV.CvEnum.DepthType.Cv32F);
            Matrix<float> transitionMatrix = new Matrix<float>(statevecSize, statevecSize);
            InitTransitionMatrix(1, ref transitionMatrix);

            Matrix<float> measurementMatrix = new Matrix<float>(dim, statevecSize);
            measurementMatrix.SetIdentity();

            Matrix<float> processNoise = new Matrix<float>(statevecSize, statevecSize);
            for (int j = 0; j < statevecSize; j++)
            {
                processNoise[j, j] = 1e-2f;

            }            

            Matrix<float> state = new Matrix<float>(statevecSize, 1);
            state.SetRandNormal(new MCvScalar(0.0f), new MCvScalar(5.0f));

            Matrix<float> errorCov = new Matrix<float>(statevecSize, statevecSize);
            errorCov.SetIdentity();

            transitionMatrix.Mat.CopyTo(kalmans[i].TransitionMatrix);
            measurementMatrix.Mat.CopyTo(kalmans[i].MeasurementMatrix);
            state.Mat.CopyTo(kalmans[i].StatePre);
            processNoise.Mat.CopyTo(kalmans[i].ProcessNoiseCov);
            errorCov.Mat.CopyTo(kalmans[i].ErrorCovPost);
        }
    }

    private void InitTransitionMatrix(int numMarkers, ref Matrix<float> transitionMatrix)
    {
        for (int i = 0; i < statevecSize; i++)
        {
            for (int j = 0; j < statevecSize; j++)
            {
                if ((int)(i / (dim * numMarkers)) == 0)
                {
                    //init top half of statevec
                    int offset = i - j;
                    switch (offset)
                    {
                        case 0:
                            transitionMatrix[i, j] = 1 + Time.deltaTime;
                            break;
                        case -3:
                            transitionMatrix[i, j] = -Time.deltaTime;
                            break;
                        default:
                            transitionMatrix[i, j] = 0.0f;
                            break;
                    }
                }
                else
                {
                    int offset = i - j;
                    switch (offset)
                    {
                        case 3: 
                            transitionMatrix[i, j] = 1.0f;
                            break;
                        default:
                            transitionMatrix[i, j] = 0.0f;
                            break;
                    }
                }
            }
        }
        //Debug.Log(StringifyMatrix(transitionMatrix));
    }
    // Update is called once per frame
    void Update()
    {
        if (markers.Length <= 0 ||
            !owl.Ready)
            return;

        switch (predictionMode)
        {
            case MarkerPredictionMode.HMM:
                UpdateMarkerPositionsMarkov();
                break;
            case MarkerPredictionMode.Kalman:
                UpdateMarkerPositionsKalman();
                break;
            case MarkerPredictionMode.KalmanGlobal:
                UpdateMarkerPositionsGlobalKalman();
                break;
            case MarkerPredictionMode.None:
                UpdateMarkerPositionsAverage();
                break;

        }
        controllingTransform.position = lastAveragePosition;
        UpdateVisualization();
    }

    private void UpdateMarkerPositionsAverage()
    {
        int count = 0;
        Vector3 avgPosition = Vector3.zero;
        for (int i = 0; i < markers.Length; i++)
        {
            int id = markers[i].id;
            if ((int)owl.Markers[id].cond < 3)
                continue;
            avgPosition += markers[i].transform.position;
            count++;
        }
        if (count == 0)
            return;
        avgPosition /= (float)count;

        lastAveragePosition = avgPosition;
        controllingTransform.position = lastAveragePosition;
    }

    private void UpdateVisualization()
    {
        if (lastPositions.Count < numSavedFrames)
        {
            lastPositions.Enqueue(lastAveragePosition);
        }
        else
        {
            lastPositions.Dequeue();
            lastPositions.Enqueue(lastAveragePosition);
        }

        lr.SetPositions(lastPositions.ToArray());
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
            Matrix<float> transitionMatrix = new Matrix<float>(statevecSize, statevecSize);
            InitTransitionMatrix(1, ref transitionMatrix);
            transitionMatrix.Mat.CopyTo(kalmans[i].TransitionMatrix);

            kalmans[i].Predict();

            Matrix<float> measurementNoise = new Matrix<float>(dim, dim);
            if ((int)owl.Markers[markers[i].id].cond < 3)
            {
                for (int j = 0; j < dim; j++)
                {
                    measurementNoise[j, j] = 1e-1f;
                }
            }
            else
            {
                for (int j = 0; j < dim; j++)
                {
                    measurementNoise[j, j] = 1e-4f;
                }
            }
            

            kalmans[i].Correct(
                new Matrix<float>(new float[]{
                    markers[i].transform.position.x,
                    markers[i].transform.position.y,
                    markers[i].transform.position.z }).Mat);
        }
        Matrix<float> sum = new Matrix<float>(statevecSize, 1);
        int count = 0;
        for (int i = 0; i < markers.Length; i++)
        {
            int id = markers[i].id;
            var matrix = kalmans[i].StatePost;
            Matrix<float> vals = new Matrix<float>(statevecSize, 1);
            matrix.ConvertTo(vals, Emgu.CV.CvEnum.DepthType.Cv32F);
            sum += vals;
            count++;
        }

        sum /= (float)count;
        lastAveragePosition = new Vector3(sum[0, 0], sum[1, 0], sum[2, 0]);
    }

    private void UpdateMarkerPositionsGlobalKalman()
    {
        int count = 0;
        Vector3 avgPosition = Vector3.zero;
        for (int i = 0; i < markers.Length; i++)
        {
            int id = markers[i].id;
            if ((int)owl.Markers[id].cond < 3)
                continue;
            avgPosition += markers[i].transform.position;
            count++;
        }

        Matrix<float> measurementNoise = new Matrix<float>(dim, dim);
        for (int j = 0; j < dim; j++)
        {
            measurementNoise[j, j] = measurementNoiseMagnitude;
        }
        measurementNoise.Mat.CopyTo(globalKalman.MeasurementNoiseCov);

        if (count == 0)
            return;
        avgPosition /= (float)count;

        Matrix<float> transitionMatrix = new Matrix<float>(statevecSize, statevecSize);
        InitTransitionMatrix(1, ref transitionMatrix);
        transitionMatrix.Mat.CopyTo(globalKalman.TransitionMatrix);



        globalKalman.Predict();
        globalKalman.Correct(new Matrix<float>(new float[] { avgPosition.x, avgPosition.y, avgPosition.z }).Mat);

        Matrix<float> statePost = new Matrix<float>(statevecSize, 1);
        globalKalman.StatePost.ConvertTo(statePost, Emgu.CV.CvEnum.DepthType.Cv32F);


        lastAveragePosition = new Vector3(statePost[0, 0], statePost[1, 0], statePost[2, 0]);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(controllingTransform.position, debugSphereSize);
    }

    public string StringifyMatrix(Matrix<float> matrix)
    {
        string output = "Matrix: \n";
        for (int i = 0; i < matrix.Rows; i++)
        {
            for (int j = 0; j < matrix.Cols; j++)
            {
                output += matrix[i, j].ToString("0.00") + "\t";
            }
            output += "\n";
        }
        return output;
    }
}

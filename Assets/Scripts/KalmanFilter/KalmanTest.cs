using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
public class KalmanTest : MonoBehaviour
{
    JointKalman kl;
    // Start is called before the first frame update
    KalmanFilter kalmanFilter;
    int statevecSize;
    void Start()
    {
        int numMarkers = 5;
        int dim = 3;
        statevecSize = numMarkers * dim * 2 + numMarkers;

        kalmanFilter = new KalmanFilter(statevecSize, numMarkers * dim, 0, Emgu.CV.CvEnum.DepthType.Cv32F);
        Debug.Log(kalmanFilter.StatePre.Rows);
        Debug.Log(kalmanFilter.StatePre.Cols);

        Matrix<float> state = new Matrix<float>(statevecSize, 1);
        state.SetRandNormal(new MCvScalar(0.0f), new MCvScalar(5.0f));
        Debug.Log(state[0, 0]);
        Debug.Log(state.Rows);
        Debug.Log(state.Cols);

        //kl = new JointKalman(numMarkers, dim, Time.deltaTime, 10.0f, Vector3.zero);
        Mat observation = new Matrix<float>(new float[] { 12, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }).Mat;
    
        Matrix<float> transitionMatrix = new Matrix<float>(statevecSize, statevecSize);
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

        Matrix<float> measurementMatrix = new Matrix<float>(numMarkers * dim, statevecSize);
        measurementMatrix.SetIdentity();

        //example: https://github.com/MultiPeden/Kalman-Filter-EMGU3.2/blob/master/Form1.cs
        //kalmanFilter.TransitionMatrix.SetTo(transitionMatrix);
        transitionMatrix.Mat.CopyTo(kalmanFilter.TransitionMatrix);
        //kalmanFilter.StatePre.SetTo(state);
        state.Mat.CopyTo(kalmanFilter.StatePost);
        state.Mat.CopyTo(kalmanFilter.StatePre);
        measurementMatrix.Mat.CopyTo(kalmanFilter.MeasurementMatrix);

        for (int i = 0; i < 100; i++)
        {
            //kl.Step(Vector<float>.Build.Dense(statevecSize, 1.0f), 0.02f);
            //Debug.Log("State: \n" + new Vector3(kl.currentState[0], kl.currentState[1], kl.currentState[2]).ToString());
            kalmanFilter.Predict();
            Matrix<float> estimated = new Matrix<float>(statevecSize, 1);
            kalmanFilter.Correct(observation).CopyTo(estimated);

            Debug.Log(estimated[0, 0]);

        }
    }

}

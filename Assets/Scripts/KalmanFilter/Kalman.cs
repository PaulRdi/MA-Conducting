using System.Collections.Generic;
using MathNet;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Storage;
using System;

/// <summary>
/// Kalman Filter vector & matrix layouts:
///     state:
///     {
///         p_x_1
///         ...
///         p_z_n
///         p_x_1_t-1
///         ...
///         p_z_n_t-1
///         alpha_1
///         ...
///         alpha_n
///     }
/// </summary>
public class JointKalman
{
    public const bool debug = true;
    public const float alpha = .99f;    //The amount with which alpha decays each frame.

    Vector<float> currentState;
    Vector<float> processNoise;
    Vector<float> measurementNoise;
    Vector<float> currentObservation;
    Matrix<float> stateTransition;
    Matrix<float> transitionControlMatrix;
    Matrix<float> observationMatrix;
    Matrix<float> processNoiseCovariance;
    Matrix<float> measurementNoiseCovariance;
    int stateVectorRows;
    int controlVecSize;
    /// <summary>
    /// Makes an observation and returns the resulting state vector.
    /// </summary>
    /// <returns></returns>
    public Matrix<float> MakeObservation(Vector<float> observation)
    {
        this.currentObservation = observation;
        return observation.ToColumnMatrix();
    }
    //A*x + B*u + w
    public Vector<float> Predict(Vector<float> state, Vector<float> control/*, Vector<float> noise*/)
    {
        return stateTransition.Multiply(state) + transitionControlMatrix.Multiply(control) /*+ noise*/;
    }
    //TODO: find process covariance!!
    public JointKalman(int numMarkers, int dimPositions, float deltaT)
    {
        //setup state vector size. Saves last and current position as well as alpha for each marker.
        stateVectorRows = numMarkers * dimPositions * 2 + numMarkers;
        controlVecSize = numMarkers;
        BuildStateTransitionMatrix(numMarkers, dimPositions, deltaT);
        BuildTransitionControlMatrix(numMarkers, dimPositions, deltaT);
        BuildObservationMatrix(numMarkers, dimPositions, deltaT);
    }

    private void BuildObservationMatrix(int numMarkers, int dimPositions, float deltaT)
    {
        List<Vector<float>> matrixRows = new List<Vector<float>>();
        int numPositions = dimPositions * numMarkers;
        for (int i = 0; i < numPositions; i++)
        {
            //use only the actual positions as part of the update 
            Vector<float> v = Vector<float>.Build.Dense(stateVectorRows);
            if (i < dimPositions*numMarkers)
                v[i] = 1;
            matrixRows.Add(v);
        }
        observationMatrix = Matrix<float>.Build.DenseOfRowVectors(matrixRows);
        if (debug) UnityEngine.Debug.Log(observationMatrix.ToString());
    }

    private void BuildTransitionControlMatrix(int numMarkers, int dimPositions, float deltaT)
    {
        //control vector sets the alpha value
        transitionControlMatrix = Matrix<float>.Build.Dense(stateVectorRows, controlVecSize);

        for (int i = 0; i < controlVecSize; i++)
        {
            transitionControlMatrix[stateVectorRows-controlVecSize+i, i] = alpha - 1.0f;
            /*
             * Updates alpha values according to control input.
             * Control input:
             * - -1 if we found a marker last frame
             * - 1 if we did not find any marker
             * 
             * Matrix (no marker)
             * |0            |
             * |0            |
             * |alpha - 1.0f |
             * 
             * Matrix (marker)
             * |0             |
             * |0             |
             * |-alpha + 1.0f |
             * --> regain confidence as marker is visible
             * --> mby better to set instantly
             * */
        }
        UnityEngine.Debug.Log(transitionControlMatrix);

    }

    private void BuildStateTransitionMatrix(int numMarkers, int dim, float deltaT)
    {
        List<Vector<float>> stateMatrixRows = new List<Vector<float>>();
        for (int i = 0; i < stateVectorRows; i++)
        {
            int xyzIdentifier = i % 3; //0 = x, 1 = y, 2 = z
            Vector<float> v = Vector<float>.Build.Dense(stateVectorRows);
            if (i < numMarkers*dim)
            {
                //we are in the first iteration of the marker set -> we are building the matrix for current marker positions.       
                v[i] = 1 + deltaT; //place on diagonal
            }
            else if (i < numMarkers * dim * 2)
            {
                //second iteration in the marker set -> we are building the matrix for last marker positions
                v[i] = -deltaT;
            }
            else
            {
                v[i] = 1.0f;
            }
            //resulting matrix:
            /*
             * |1+dt 0   0|
             * |0    -dt 0|
             * |0    0   1|
             * 
             * gets applied for state vec
             * |pn_t    |
             * |pn_t-1  |
             * |alpha_n |
             * 
             * effect:
             * extrapolate pn_t according to heading.
             * */
            stateMatrixRows.Add(v);
        }
        stateTransition = Matrix<float>.Build.DenseOfRowVectors(stateMatrixRows);
        if (debug) UnityEngine.Debug.Log(stateTransition.ToString());
    }

}

using System.Collections.Generic;
using MathNet;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Storage;
using MathNet.Numerics.Distributions;
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
    Matrix<float> processCovarianceNoise;
    Matrix<float> measurementCovarianceNoise;
    Matrix<float> forecastErrorCovariance;
    Matrix<float> processCovariance;
    int stateVectorRows;
    int numMarkers;
    int dim;

    //TODO: find process covariance!!
    public JointKalman(int numMarkers, int dimPositions, float deltaT, float roomSize, UnityEngine.Vector3 origin)
    {
        this.dim = dimPositions;
        this.numMarkers = numMarkers;
        //setup state vector size. Saves last and current position as well as alpha for each marker.
        this.stateVectorRows = numMarkers * dimPositions * 2 + numMarkers;
        this.processCovarianceNoise = Matrix<float>.Build.Dense(stateVectorRows, stateVectorRows, 0.0f);
        this.measurementCovarianceNoise = Matrix<float>.Build.Dense(dimPositions * numMarkers, dimPositions * numMarkers, 0.0f);
        //generate the initial state according to normal distribution.
        Vector<float> initialState = Vector<float>.Build.Dense(stateVectorRows);
        Vector<float> idealState = Vector<float>.Build.Dense(stateVectorRows);

        

        double[] rndvec = new double[numMarkers * dimPositions * 2];
        Normal gauss = new Normal(0.0d, roomSize); //initial standard deviation means positions can be distributed across entire room
        gauss.Samples(rndvec);
        for (int i = 0; i < rndvec.Length; i++)
        {
            float originComponent = 0.0f;
            switch (i % 3)
            {
                case 0:
                    originComponent = origin.x;
                    break;
                case 1:
                    originComponent = origin.y;
                    break;
                case 2:
                    originComponent = origin.z;
                    break;
            }
            initialState[i] = originComponent + (float)rndvec[i]; //transform initial positions to origin
            idealState[i] = originComponent;
        }
        for (int i = 2 * dimPositions * numMarkers; i < stateVectorRows; i++)
        {
            initialState[i] = 1.0f;
            idealState[i] = 1.0f;
        }
        currentState = initialState;
        Matrix<float> rowMatrix = (currentState - idealState).ToColumnMatrix();
        processCovariance = rowMatrix.Multiply(rowMatrix.Transpose());

        BuildStateTransitionMatrix(numMarkers, dimPositions, deltaT);
        BuildTransitionControlMatrix(numMarkers, dimPositions, deltaT);
        BuildObservationMatrix(numMarkers, dimPositions, deltaT);
    }

    /// <summary>
    /// Represents one step in the discrete kalman filter 
    /// </summary>
    /// <param name="observation"></param>
    /// <returns></returns>
    public Vector<float> Step(Vector<float> observation, float deltaTime)
    {
        BuildStateTransitionMatrix(numMarkers, dim, deltaTime);
        BuildTransitionControlMatrix(numMarkers, dim, deltaTime);

        Vector<float> predictedState = Predict(
            currentState, 
            Vector<float>.Build.Dense(numMarkers, 0f), 
            Vector<float>.Build.Dense(stateVectorRows, 0f));

        Matrix<float> forecastCovariance = GetForecastErrorCovariance();

        Matrix<float> d = observationMatrix.Multiply(forecastCovariance);
        d = d.Multiply(observationMatrix.Transpose()) + measurementCovarianceNoise;
        
        //Todo: figure out proper matrix formatting.
        Matrix<float> kalmanGain = (forecastCovariance.Multiply(observationMatrix)).Multiply(d.Inverse());
        Vector<float> observedState = MakeObservation(observation);

        Vector<float> resultingState = predictedState + kalmanGain.Multiply(observedState - observationMatrix.Multiply(predictedState));


        //compute P_k
        Matrix<float> finalCovariance = (Matrix<float>.Build.DenseIdentity(stateVectorRows) - kalmanGain.Multiply(observationMatrix)).Multiply(forecastCovariance);

        processCovariance = finalCovariance;
        currentState = resultingState;

        return resultingState;
    }

    /// <summary>
    /// Makes an observation and returns the resulting state vector.
    /// 
    /// </summary>
    /// <returns></returns>
    public Vector<float> MakeObservation(Vector<float> observation)
    {
        return observationMatrix.Multiply(observation) + measurementNoise;
    }
    //A*x + B*u + w
    public Vector<float> Predict(Vector<float> state, Vector<float> control, Vector<float> noise)
    {
        return stateTransition.Multiply(state) + transitionControlMatrix.Multiply(control) + noise;
    }

    public Matrix<float> GetForecastErrorCovariance()
    {
        Matrix<float> tmp = stateTransition.Multiply(processCovariance);
        return tmp.Multiply(stateTransition.Transpose()) + processCovarianceNoise;
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
        if (debug) UnityEngine.Debug.Log("Observation matrix: " + "\n" + observationMatrix.ToString());
    }

    private void BuildTransitionControlMatrix(int numMarkers, int dimPositions, float deltaT)
    {
        //control vector sets the alpha value
        transitionControlMatrix = Matrix<float>.Build.Dense(stateVectorRows, numMarkers);

        for (int i = 0; i < numMarkers; i++)
        {
            transitionControlMatrix[stateVectorRows- numMarkers + i, i] = alpha - 1.0f;
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
        if (debug) UnityEngine.Debug.Log("Transition control matrix: \n" + transitionControlMatrix.ToString());

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
        if (debug) UnityEngine.Debug.Log("State Transitioin Matrix: \n " + stateTransition.ToString());
    }

}

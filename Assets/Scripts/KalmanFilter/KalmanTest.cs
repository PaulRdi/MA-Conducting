using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
public class KalmanTest : MonoBehaviour
{
    JointKalman kl;
    // Start is called before the first frame update
    void Start()
    {
        int numMarkers = 5;
        int dim = 3;
        int statevecSize = numMarkers * dim * 2 + numMarkers;
        kl = new JointKalman(numMarkers, dim, Time.deltaTime, 10.0f, Vector3.zero);
        Debug.Log(kl.Predict(Vector<float>.Build.Dense(statevecSize, 1.0f),
            Vector<float>.Build.Dense(numMarkers, -1.0f)));

    }

    
}

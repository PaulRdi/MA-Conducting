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
       


        for (int i = 0; i < 100; i++)
        {
            kl.Step(Vector<float>.Build.Dense(statevecSize, 1.0f), 0.02f);
        }
    }

    
}

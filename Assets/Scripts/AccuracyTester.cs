using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ComputeParticleFilter))]
public class AccuracyTester : MonoBehaviour
{
    public int maxNumValues = 100;
    public List<int> recordedValues;
    ComputeParticleFilter particlefilter;
    // Start is called before the first frame update
    void Start()
    {
        this.particlefilter = GetComponent<ComputeParticleFilter>();

    }

    // Update is called once per frame
    void Update()
    {
        recordedValues.Add(particlefilter.numResamples);
        if (recordedValues.Count >= maxNumValues)
            recordedValues.RemoveAt(0);
    }
}

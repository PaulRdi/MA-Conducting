using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleFilter))]
public class AccuracyTester : MonoBehaviour
{
    public int maxNumValues = 100;
    public List<int> recordedValues;
    ParticleFilter particlefilter;
    // Start is called before the first frame update
    void Start()
    {
        this.particlefilter = GetComponent<ParticleFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        recordedValues.Add(particlefilter.lastNumberOfResamples);
        if (recordedValues.Count >= maxNumValues)
            recordedValues.RemoveAt(0);
    }
}

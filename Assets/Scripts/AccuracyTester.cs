using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(ParticleFilter))]
public class AccuracyTester : MonoBehaviour
{
    public int maxNumValues = 100;
    public List<int> recordedValues;
    public int framesForAverage = 60;
    public double lastAverage;
    public List<double> recordedAverages;

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
        if (recordedValues.Count >= TestConfig.current.maxRecordedValues)
            recordedValues.RemoveAt(0);

        lastAverage = recordedValues.Skip(Math.Max(0, recordedValues.Count - framesForAverage)).Average();
        recordedAverages.Add(lastAverage);
        if (recordedAverages.Count >= TestConfig.current.maxRecordedValues)
            recordedAverages.RemoveAt(0);
    }
}

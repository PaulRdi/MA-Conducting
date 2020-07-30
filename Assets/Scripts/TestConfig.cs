using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName=  "testConfig.asset")]
public class TestConfig : ScriptableObject
{

    /// <summary>
    /// The time a beat is valid in milliseconds
    /// </summary>
    public double beatBuffer => _beatBuffer;
    [SerializeField] double _beatBuffer = 50;

    public float motionAccuracy => _motionAccuracy;
    [Range(0f, 1f)]
    [SerializeField]
    float _motionAccuracy = 1.0f;

    public static TestConfig current;
}

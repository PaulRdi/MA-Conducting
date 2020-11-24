using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName=  "testConfig.asset")]
public class TestConfig : ScriptableObject
{

    /// <summary>
    /// The time a beat is valid in seconds
    /// </summary>
    public double beatBuffer => _beatBuffer;
    [SerializeField] double _beatBuffer = 50;

    public float motionAccuracy => _motionAccuracy;
    [Range(0f, 1f)]
    [SerializeField]
    float _motionAccuracy = 1.0f;

    public double startOffset => _startOffet;
    [SerializeField] double _startOffet = 1.0;

    public static TestConfig current
    {
        get
        {
            if (_current == null)
                _current = Resources.Load<TestConfig>("fallbackTestConfig");
            return _current;
        }
        set
        {
            _current = value;
        }
    }
    static TestConfig _current;
}

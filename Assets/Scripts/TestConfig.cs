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

    public int particleDeathCutoff => _particleDeathCutoff;
    [SerializeField] int _particleDeathCutoff = 1200;

    public int numParticles => _numParticles;
    [SerializeField] int _numParticles = 8192;

    public double maxAccuracyLossPerSecond => _maxAccuracyLossPerSecond;
    [SerializeField] double _maxAccuracyLossPerSecond = .2f;

    public double maxAccuracyGainPerSecond => _maxAccuracyGainPerSecond;
    [SerializeField] double _maxAccuracyGainPerSecond = .2f;

    public int maxRecordedValues => _maxRecordedValues;
    [SerializeField] int _maxRecordedValues = 1000;

    public float graphLineWidth => _graphLineWidth;
    [SerializeField] float _graphLineWidth = 3.0f;

    public float correctBeatAccuracyBonus => _correctBeatAccuracyBonus;
    [SerializeField] float _correctBeatAccuracyBonus = .05f;

    public float missedBeatPenalty => _missedBeatPenalty;
    [SerializeField] float _missedBeatPenalty = 0.05f;

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

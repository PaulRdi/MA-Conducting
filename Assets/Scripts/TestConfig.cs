using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "testConfig.asset")]
public class TestConfig : ScriptableObject
{
    public bool debug => _debug;
    [SerializeField] bool _debug = false;

    public double beatBuffer => _beatBuffer;
    [SerializeField] double _beatBuffer = 50;

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

    public float accelerationThreshold => _accelerationThreshold;
    [SerializeField] float _accelerationThreshold = .04f;

    public float beatTime => _beatTime;
    [SerializeField] float _beatTime = .05f;

    public int beatNumVelocityEntries => _beatNumVelocityEntries;
    [SerializeField] int _beatNumVelocityEntries = 10;

    public float beatMinAngleVelocity => _beatMinAngleVelocity;
    [SerializeField] float _beatMinAngleVelocity = .05f;

    public float particleSpeedMod => _particleSpeedMod;
    [SerializeField] float _particleSpeedMod = 1.0f;

    public float jointGravity => _jointGravity;
    [SerializeField] float _jointGravity = 0.6f;

    public float maxG => _maxG;
    [SerializeField] float _maxG = .1f;

    public int moveAccuracyNumFrames => _moveAccuracyNumFrames;
    [SerializeField] int _moveAccuracyNumFrames = 60;

    public Vector2 particleMinMaxDist => _particleMinMaxDist;
    [SerializeField] Vector2 _particleMinMaxDist = new Vector2(.06f, .2f);

    public Vector2 minMaxLightStrength => _minMaxLightStrength;
    [SerializeField] Vector2 _minMaxLightStrength = new Vector2(1.2f, 4.0f);

    public float minFrequency => _minFrequency;
    [SerializeField] float _minFrequency = 2000.0f;

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

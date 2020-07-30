using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeedbackEvaluator : MonoBehaviour
{   

    List<float> protocolledBeats;

    void OnEnable()
    {
        TestManager.correctBeatDetected += TestManager_correctBeatDetected;
        TestManager.falseBeatDetected += TestManager_falseBeatDetected;
        TestManager.testStarted += TestManager_testStarted;
    }    

    void OnDisable()
    {
        TestManager.correctBeatDetected -= TestManager_correctBeatDetected;
        TestManager.falseBeatDetected -= TestManager_falseBeatDetected;
        TestManager.testStarted -= TestManager_testStarted;

    }

    private void TestManager_testStarted()
    {
    }

    private void TestManager_falseBeatDetected()
    {
    }

    private void TestManager_correctBeatDetected()
    {
    }
}

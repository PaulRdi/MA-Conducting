using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class BeatDetector : MonoBehaviour
{
    public static Action<BeatDetector> beatDetected;

    Vector3 lastPos;
    Vector3 lastVel;
    Vector3 vel;
    Vector3 deltaVel;
    Coroutine beatRoutine;


    public float accelerationThreshold = 1.0f;
    public float beatTime = 0.5f;
    [SerializeField] Transform phaseSpace;
    Quaternion relativeRotationToPhaseSpace;
    Func<bool> condition;

    void Start()
    {
        lastPos = transform.position;
        lastVel = Vector3.zero;

        relativeRotationToPhaseSpace = Quaternion.FromToRotation(transform.forward, phaseSpace.forward);


        condition = () =>
        {
            return deltaVel.magnitude > accelerationThreshold;
        };
        //condition = () =>
        //{
        //    Vector3 rotatedVel = relativeRotationToPhaseSpace * deltaVel;
        //    return Mathf.Abs(rotatedVel.y) > accelerationThreshold;
        //};
    }

    // Update is called once per frame
    void Update()
    {
        if (TestManager.instance == null ||
            !TestManager.instance.songRunning)
            return;
        vel = transform.position - lastPos;
        deltaVel = vel - lastVel;

        if (beatRoutine == null &&
            condition.Invoke())
            beatRoutine = StartCoroutine(DetectedBeatRoutine());

        lastPos = transform.position;

        lastVel = vel;
    }

    private void LateUpdate()
    {
    }

    IEnumerator DetectedBeatRoutine()
    {
        beatDetected?.Invoke(this);
        yield return new WaitForSeconds(beatTime);
        beatRoutine = null;
    }
}

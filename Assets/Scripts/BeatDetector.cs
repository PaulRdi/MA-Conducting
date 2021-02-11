using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

public class BeatDetector : MonoBehaviour
{
    public static event Action<BeatDetector> beatDetected;

    Vector3 lastPos;
    Vector3 lastVel;
    Vector3 vel;
    Vector3 deltaVel;
    Coroutine beatRoutine;


    public float accelerationThreshold = 1.0f;
    public float beatTime = 0.5f;
    public int numVelocityEntries = 10;
    Func<bool> condition;
    List<float> recordedVelocityDeltas;

    ParticleSystem system;
    private void Awake()
    {
        system = GetComponent<ParticleSystem>();
    }
    void Start()
    {
        recordedVelocityDeltas = new List<float>();
        lastPos = transform.position;
        lastVel = Vector3.zero;



        condition = () =>
        {
            return recordedVelocityDeltas.Average() > accelerationThreshold;
        };
        //condition = () =>
        //{
        //    return deltaVel.magnitude > accelerationThreshold;
        //};
        //condition = () =>
        //{
        //    Vector3 rotatedVel = relativeRotationToPhaseSpace * deltaVel;
        //    return Mathf.Abs(rotatedVel.y) > accelerationThreshold;
        //};
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
        vel = transform.position - lastPos;
        deltaVel = vel - lastVel;
        recordedVelocityDeltas.Add(deltaVel.magnitude);
        if (recordedVelocityDeltas.Count >= numVelocityEntries)
            recordedVelocityDeltas.RemoveAt(0);
        if (beatRoutine == null &&
            condition.Invoke())
            beatRoutine = StartCoroutine(DetectedBeatRoutine());

        lastPos = transform.position;
        lastVel = vel;
    }

    IEnumerator DetectedBeatRoutine()
    {
        if (system != null)
            system.Emit(1);
        beatDetected?.Invoke(this);
        Debug.Log("beat");
        yield return new WaitForSeconds(beatTime);
        beatRoutine = null;
    }
}

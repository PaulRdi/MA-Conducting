using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class ParticleFilter : MonoBehaviour
{
    [SerializeField] string fileName;
    [SerializeField] ComputeShader particleFilter;
    [SerializeField] Transform[] trackingBones;
    [SerializeField] Transform[] recordedBones;
    [SerializeField] Transform min, max;
    [SerializeField] Transform recordedBonesOrigin, trackingBonesOrigin;

    [SerializeField] int numParticles = 8192;
    [SerializeField] float speed = 1.0f;
    [SerializeField] float gravity = 100.0f;
    [SerializeField] ParticleSystem trackedParticles;
    [SerializeField] Vector2 minMaxDist;

    Dictionary<int, Vector3[]> recordedData;
    F_Particle[] particles;

    int frameCounter = 0;

    public int lastNumberOfResamples;
    public List<int> recordedValues;
    int maxSamples = 1000;

    void Start()
    {
        recordedValues = new List<int>();
        Debug.Log(trackingBones.Length);
        particles = new F_Particle[numParticles];
        for (int i = 0; i < numParticles; i++)
        {
            float rx = UnityEngine.Random.value;
            float ry = UnityEngine.Random.value;
            float rz = UnityEngine.Random.value;

            particles[i] = new F_Particle();

            particles[i].weight = 1.0f;
            particles[i].position = new Vector3(
                Mathf.Lerp(min.position.x, max.position.x, rx),
                Mathf.Lerp(min.position.y, max.position.y, ry),
                Mathf.Lerp(min.position.z, max.position.z, rz));
        }
    }

    void Update()
    {
        GPU_PARTICLES();
        //CPU_PARTICLES();
        int numResamples = 0;
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i].resampleFlag == 1)
            {
                numResamples++;
            }
            particles[i].resampleFlag = 0;
        }

        if (trackedParticles != null)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = particles[i].position;
                emitParams.startLifetime = 0.1f;
                emitParams.startSize = .1f;
                trackedParticles.Emit(emitParams, 1);
            }
        }

        lastNumberOfResamples = numResamples;

    }

    private void GPU_PARTICLES()
    {
        ComputeBuffer particleBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(F_Particle)));
        int kernelID = particleFilter.FindKernel("FilterParticles");
        UpdateComputeShaderData(kernelID, particleBuffer);

        int numThreads = numParticles / 512;
        particleFilter.Dispatch(kernelID, numThreads, 1, 1);

        particleBuffer.GetData(particles);
        particleBuffer.Dispose();
    }

    public void ResetFrames()
    {
        frameCounter = 0;
    }
    void UpdateComputeShaderData(int kernelID, ComputeBuffer particleBuffer)
    {
        particleBuffer.SetData(particles, 0, 0, numParticles);
        particleFilter.SetBuffer(kernelID, "particles", particleBuffer);
        particleFilter.SetFloat("deltaTime", Time.deltaTime);
        particleFilter.SetFloat("speed", speed);
        particleFilter.SetFloat("gravity", gravity);
        particleFilter.SetFloat("minDist", minMaxDist.x);
        particleFilter.SetFloat("maxDist", minMaxDist.y);

        particleFilter.SetVectorArray("recordedBones", GetVectorArray(recordedBones.Select(b => (Vector4)b.position)));
        particleFilter.SetInt("numRecordedBones", recordedBones.Length);
        particleFilter.SetVector("recordedBonesOrigin", recordedBonesOrigin.position);

        particleFilter.SetVectorArray("trackingBones", GetVectorArray(trackingBones.Select(b => (Vector4)b.position)));
        particleFilter.SetInt("numTrackingBones", trackingBones.Length);
        particleFilter.SetVector("trackingBonesOrigin", trackingBonesOrigin.position);

    }
    private Vector4[] GetVectorArray(IEnumerable<Vector4> enumerable)
    {
        Vector4[] arr = new Vector4[128];
        int i = 0;
        foreach (Vector4 v in enumerable)
        {
            arr[i] = v;
            i++;
        }
        for (int j = i; j < arr.Length; j++)
        {
            arr[j] = new Vector4(0, 0, 0);
        }
        return arr;
    }


    void CPU_PARTICLES()
    {
        for (int id = 0; id < particles.Length; id++)
        {
            Vector3 distanceSum = new Vector3(0, 0, 0);
            Vector3 dir = new Vector3(0, 0, 0);
            Vector3 closestDir = new Vector3(9999, 9999, 9999);

            Vector3 centeredParticlePosition = particles[id].position - trackingBonesOrigin.position;
            int boneID = id % trackingBones.Length;

            Vector3 tBonePos = recordedBones[boneID].position - recordedBonesOrigin.position;
            Vector3 d = tBonePos - centeredParticlePosition;
            float l = d.magnitude;
            distanceSum += d;
            dir += d * gravity * (1.0f / (l * l));

            if (l < closestDir.magnitude)
            {
                closestDir = d;
            }

            float distanceToClosest = closestDir.magnitude;
            particles[id].position += closestDir.normalized * speed * Time.deltaTime;

            Vector3 bonePos = trackingBones[boneID].position - trackingBonesOrigin.position;
            d = bonePos - (particles[id].position - trackingBonesOrigin.position);
            l = d.magnitude;

            if (l < closestDir.magnitude)
            {
                closestDir = d;
            }

            distanceToClosest = closestDir.magnitude;
            //reweight
            float weight = 1.0f - Mathf.Min((distanceToClosest - minMaxDist.x) / (minMaxDist.y - minMaxDist.x), 1.0f);
            //weight now is between 0.0 and 1.0 between minDist and maxDist
            //if weight is high -> particle was close to nearest joint.
            //With this -> resample particle if it died


            //float a = (1 / (2*PI)) * exp(-(distanceToClosest*distanceToClosest) / 2);

            //closestBoneIndex = min(closestBoneIndex, numBones);
            if (weight < UnityEngine.Random.Range(0f, 1f))
            {
                //particles[id].position = Resample(particles[id].position, closestDir, bones[closestBoneIndex].position);
                particles[id].position = Resample(
                    particles[id].position - trackingBonesOrigin.position,
                    closestDir,
                    trackingBones[boneID].position - trackingBonesOrigin.position);
                particles[id].position += trackingBonesOrigin.position;
                particles[id].resampleFlag = 1;
            }
        }
    }

    Vector3 Resample(Vector3 lastPos, Vector3 closestDir, Vector3 bone)
    {
        return bone + SampleSphereCoord(new Vector2(closestDir.y, closestDir.z) * UnityEngine.Random.Range(0f, 1.0f) * minMaxDist.x);

        //return bones[closestBoneIndex] - closestDir * random(closestDir.xy);
    }
    //http://corysimon.github.io/articles/uniformdistn-on-sphere/
    Vector3 SampleSphereCoord(Vector2 seed)
    {
        float theta = 2.0f * Mathf.PI * UnityEngine.Random.Range(0f, 1.0f);
        float phi = Mathf.Acos(1.0f - 2.0f * UnityEngine.Random.Range(0f, 1.0f));

        float x = Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = Mathf.Cos(phi);
        return new Vector3(x, y, z);
    }
}

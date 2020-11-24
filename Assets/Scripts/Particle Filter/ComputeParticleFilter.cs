using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Runtime.InteropServices;
struct F_Particle
{
    public Vector3 position;
    public float weight;
    public int resampleFlag;
}
public class ComputeParticleFilter : MonoBehaviour
{


    [SerializeField] int numParticles;
    [SerializeField] Transform min, max;
    [SerializeField] ComputeShader particleFilter;
    [SerializeField] Transform[] boneTransforms;
    [SerializeField] ParticleSystem visParticles;
    [SerializeField] float speed = 1.0f;
    [SerializeField] float gravity = 100.0f;
    [SerializeField] Vector2 minMaxDist;
    F_Particle[] particles;

    public int numResamples;

    // Start is called before the first frame update
    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {
        ComputeBuffer particleBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(F_Particle)));
        //ComputeBuffer bonesBuffer = new ComputeBuffer(boneTransforms.Length, Marshal.SizeOf(typeof(Vector3)));
        //ComputeBuffer targetBonesBuffer = new ComputeBuffer(boneTransforms.Length, Marshal.SizeOf(typeof(Vector3)));
        int kernelID = particleFilter.FindKernel("FilterParticles");
        //UpdateComputeShaderData(kernelID, particleBuffer, bonesBuffer, targetBonesBuffer);
        UpdateComputeShaderData(kernelID, particleBuffer);

        int numThreads = numParticles / 1024;
        particleFilter.Dispatch(kernelID, numThreads, 1, 1);

        particleBuffer.GetData(particles);

        numResamples = 0;
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i].resampleFlag == 1)
            {
                numResamples++;
            }
        }
        if (visParticles != null)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = particles[i].position;
                emitParams.startLifetime = 0.1f;
                emitParams.startSize = .1f;
                visParticles.Emit(emitParams, 1);
                
                particles[i].resampleFlag = 0;
            }
        }
        particleBuffer.Dispose();
        //bonesBuffer.Dispose();
        //targetBonesBuffer.Dispose();
    }

    void UpdateComputeShaderData(int kernelID, ComputeBuffer particleBuffer, ComputeBuffer bonesBuffer = null, ComputeBuffer targetBonesBuffer = null)
    {
        particleBuffer.SetData(particles, 0, 0, numParticles);
        particleFilter.SetBuffer(kernelID, "particles", particleBuffer);

        particleFilter.SetFloat("deltaTime", Time.deltaTime);
        particleFilter.SetFloat("speed", speed);
        particleFilter.SetFloat("gravity", gravity);
        particleFilter.SetFloat("minDist", minMaxDist.x);
        particleFilter.SetFloat("maxDist", minMaxDist.y);


        //Vector3[] b1 = boneTransforms.Select(b => b.position).ToArray();
        //bonesBuffer.SetData(b1, 0, 0, b1.Length);
        //particleFilter.SetBuffer(kernelID, "bones", bonesBuffer);

        //Vector3[] b2 = boneTransforms.Select(b => b.position).ToArray();
        //targetBonesBuffer.SetData(b2, 0, 0, b2.Length);
        //particleFilter.SetBuffer(kernelID, "targetBones", targetBonesBuffer);

        particleFilter.SetVectorArray("bones", GetVectorArray(boneTransforms.Select(b => (Vector4)b.position)));
        particleFilter.SetVectorArray("targetBones", GetVectorArray(boneTransforms.Select(b => (Vector4)b.position)));
        particleFilter.SetInt("numBones", boneTransforms.Length);
        particleFilter.SetInt("numTargetBones", boneTransforms.Length);

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
}

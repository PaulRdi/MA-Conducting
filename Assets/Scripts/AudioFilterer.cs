using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
public class AudioFilterer : MonoBehaviour
{
    float maxFrequency;
    float defaultFF;
    [SerializeField] AudioMixer mixer;
    TestManagerVersion2 tm;
    private void Awake()
    {
        maxFrequency = 14000.0f;
        tm = GetComponent<TestManagerVersion2>();
        if (!mixer.GetFloat("lowpass", out defaultFF))
            throw new System.Exception("cound not find audio mixer value lowpass");


    }


    // Update is called once per frame
    void Update()
    {
        if (tm.testState == TestState.Running)
        {
            mixer.SetFloat("lowpass", Mathf.Lerp(TestConfig.current.minFrequency, maxFrequency,Mathf.Min(1.0f,1-Mathf.Max(0, 1+((Mathf.Exp(-(float)tm.currentAccuracy)-1.0f)* 1.64f)))));
        }
    }
}

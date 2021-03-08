using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
[RequireComponent(typeof(Light))]
public class LightController : MonoBehaviour
{
    HDAdditionalLightData l;
    private void Awake()
    {
        l = GetComponent<HDAdditionalLightData>();
    }


    // Update is called once per frame
    void Update()
    {
        l.SetSpotLightLuxAt(l.intensity, Mathf.Lerp(TestConfig.current.minMaxLightStrength.x, TestConfig.current.minMaxLightStrength.y, (float)TestManagerVersion2.instance.currentAccuracy));
    }
}

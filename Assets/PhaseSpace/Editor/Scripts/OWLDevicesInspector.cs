using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PhaseSpace.Unity;

[CustomEditor(typeof(OWLDevices))]
public class OWLDevicesInspector : Editor
{

    SerializedProperty devices, locked;
    private void OnEnable()
    {
        devices = serializedObject.FindProperty("Devices");
        locked = serializedObject.FindProperty("Locked");
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(locked);
        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        {
            EditorGUILayout.PropertyField(devices, true);
        }
        EditorGUI.EndDisabledGroup();




        if (serializedObject.ApplyModifiedProperties())
        {
            //serialized
        }
    }
}

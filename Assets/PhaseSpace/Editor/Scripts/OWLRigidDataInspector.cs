/*
* Copyright 2017, PhaseSpace Inc.
* 
* Material contained in this software may not be copied, reproduced to any electronic medium or 
* machine readable form or otherwise duplicated and the information herein may not be used, 
* disseminated or otherwise disclosed, except with the prior written consent of an authorized 
* representative of PhaseSpace Inc.
*
* PhaseSpace and the PhaseSpace logo are registered trademarks, and all PhaseSpace product 
* names are trademarks of PhaseSpace Inc.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
namespace PhaseSpace.Unity
{
    [CustomEditor(typeof(OWLRigidData))]
    public class OWLRigidDataInspector : Editor
    {

        OWLRigidData rigidData;
        static float scale = 0.001f;
        Vector3[] transformedPoints = new Vector3[0];
        Transform origin;
        bool editMode;
        OWLClient client;

        public Vector3 posOffset = Vector3.zero;
        public Vector3 eulerOffset = Vector3.zero;

        Vector3 GetPoint(int index)
        {
            Vector3 v = rigidData.points[index];
            v.z *= -1;
            v *= scale;

            return v;
        }

        void SetPoint(int index, Vector3 v)
        {
            v.z *= -1;
            v /= scale;

            rigidData.points[index] = v;
        }

        void UpdateTransformedPoints()
        {
            if (transformedPoints.Length != rigidData.points.Length)
            {
                transformedPoints = new Vector3[rigidData.points.Length];
            }

            for (int i = 0; i < rigidData.points.Length; i++)
            {
                if (origin != null)
                {
                    //transformedPoints[i] = origin.TransformPoint(rotOffset * (GetPoint(i) + posOffset));
                    transformedPoints[i] = origin.TransformPoint((Quaternion.Euler(eulerOffset) * GetPoint(i)) + posOffset);
                }
                else
                {
                    transformedPoints[i] = (Quaternion.Euler(eulerOffset) * GetPoint(i)) + posOffset;
                }

            }
        }

        Vector3 AveragePosition
        {
            get
            {
                Vector3 c = Vector3.zero;
                foreach (var v in transformedPoints)
                {
                    c += v;
                }
                c /= transformedPoints.Length;

                return c;
            }
        }

        void OnEnable()
        {
            if (Application.isPlaying)
                client = FindObjectOfType<OWLClient>();


            rigidData = (OWLRigidData)target;

#if UNITY_2019_1_OR_NEWER
	    SceneView.duringSceneGui += OnSceneView;
#else
	    SceneView.onSceneGUIDelegate += OnSceneView;
#endif

	    UpdateTransformedPoints();
        }

        void OnDisable()
        {
#if UNITY_2019_1_OR_NEWER
	    SceneView.duringSceneGui -= OnSceneView;
#else
	    SceneView.onSceneGUIDelegate -= OnSceneView;
#endif
	}

        void OnSceneView(SceneView sv)
        {
            if (editMode)
            {

                Event e = Event.current;
                if (e.type == EventType.KeyUp)
                {
                    if (e.keyCode == KeyCode.F)
                    {
                        sv.pivot = AveragePosition;
                        e.Use();
                    }

                }


                if (origin == null || Selection.objects.Contains(origin.gameObject) == false)
                    OffsetHandles();
                else
                {
                    OWLEditorUtilities.DrawAxes(origin.TransformPoint(posOffset), Quaternion.Euler(eulerOffset) * origin.rotation);
                }

                UpdateTransformedPoints();
                if (transformedPoints.Length == rigidData.ids.Length)
                    OWLEditorUtilities.DrawMarkers(transformedPoints, rigidData.ids);


                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(0, 0, 512, 512));
                GUILayout.Label("Rigidbody Editor", PhaseSpaceStyles.GiantSceneViewLabel);
                GUILayout.BeginHorizontal();
                DrawEditButton();
                DrawPushButton();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }

        void OffsetHandles()
        {
            Vector3 pos = posOffset;
            Quaternion rot = Quaternion.Euler(eulerOffset);
            if (origin != null)
            {
                pos = origin.TransformPoint(pos);
                rot = rot * origin.rotation;
            }

            EditorGUI.BeginChangeCheck();
            switch (Tools.current)
            {
                case Tool.Move:
                    pos = Handles.PositionHandle(pos, Tools.pivotRotation == PivotRotation.Local ? rot : Quaternion.identity);
                    break;
                case Tool.Rotate:
                    rot = Handles.RotationHandle(rot, pos);
                    break;
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Rigid Editor Offset");
                Repaint();
                if (origin != null)
                {
                    posOffset = origin.InverseTransformPoint(pos);
                    eulerOffset = (Quaternion.Inverse(origin.rotation) * rot).eulerAngles;
                }
                else
                {
                    posOffset = pos;
                    //rotOffset = rot;
                    eulerOffset = rot.eulerAngles;
                }
            }


        }

        public override void OnInspectorGUI()
        {
            DrawTopToolbar();

            if (editMode)
            {
                DrawEditingTools();
            }
            else
            {
                base.OnInspectorGUI();
            }
        }

        void DrawTopToolbar()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(editMode);
                if (GUILayout.Button("Import", EditorStyles.miniButtonLeft, GUILayout.Width(64)))
                {
                    string path = EditorUtility.OpenFilePanel("Load Rigidbody JSON", EditorPrefs.GetString("LastRBLoadPath", Application.dataPath), "json");
                    if (path != "")
                    {
                        EditorPrefs.SetString("LastRBLoadPath", Path.GetDirectoryName(path));
                        rigidData.LoadFromFile(path);
                        EditorUtility.SetDirty(rigidData);
                    }
                }

                if (GUILayout.Button("Export", EditorStyles.miniButtonRight, GUILayout.Width(64)))
                {
                    //string path = EditorUtility.OpenFilePanel("Load Rigidbody JSON", EditorPrefs.GetString("LastRBLoadPath", Application.dataPath), "json");
                    string path = EditorUtility.SaveFilePanel("Export Rigidbody JSON", EditorPrefs.GetString("LastRBLoadPath", Application.dataPath), rigidData.name, "json");
                    if (path != "")
                    {
                        EditorPrefs.SetString("LastRBLoadPath", Path.GetDirectoryName(path));

                        string json = rigidData.GetJSON();
                        File.WriteAllText(path, json);
                    }
                }
                EditorGUI.EndDisabledGroup();

                DrawEditButton();
                DrawPushButton();
            }
            GUILayout.EndHorizontal();
        }

        void Recapture()
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < rigidData.ids.Length; i++)
            {
                var id = (int)rigidData.ids[i];

                if (client.Markers[id].Condition > TrackingCondition.Poor)
                {
                    Vector3 pos = client.Markers[id].position;
                    points.Add(origin.InverseTransformPoint(pos));
                }
            }

            if (points.Count == rigidData.ids.Length)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    SetPoint(i, points[i]);
                }
            }
            else
            {
                Debug.LogWarning("Recapture failed!");
            }
        }

        void DrawPushButton()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying == false || client == null || client.State < OWLClient.ConnectionState.Initialized || client.SlaveMode == SlaveMode.Slave);
            {
                if (GUILayout.Button("Push", EditorStyles.miniButton, GUILayout.Width(64)))
                    client.CreateRigidTracker(rigidData);
                if (editMode)
                {
                    EditorGUI.BeginDisabledGroup(origin == null);
                    {
                        if (GUILayout.Button("Recapture", EditorStyles.miniButton, GUILayout.Width(64)))
                        {
                            Recapture();
                        }
                    }




                }
            }
            EditorGUI.EndDisabledGroup();
        }

        void DrawEditButton()
        {
            if (!editMode)
            {
                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(64)))
                {
                    StartEdit();
                    SceneView.RepaintAll();
                    Repaint();
                }
            }
            else
            {
                if (GUILayout.Button("Apply", EditorStyles.miniButtonLeft, GUILayout.Width(64)))
                {
                    ApplyEdit();
                    SceneView.RepaintAll();
                    Repaint();
                }
                if (GUILayout.Button("Done", EditorStyles.miniButtonRight, GUILayout.Width(64)))
                {
                    EndEdit();
                    SceneView.RepaintAll();
                    Repaint();
                }
            }
        }

        void DrawEditingTools()
        {
            EditorGUILayout.LabelField("Edit Mode", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            origin = (Transform)EditorGUILayout.ObjectField("Origin", origin, typeof(Transform), true);

            EditorGUI.BeginDisabledGroup(false);
            posOffset = EditorGUILayout.Vector3Field("Position Offset", posOffset);
            eulerOffset = EditorGUILayout.Vector3Field("Rotation Offset", eulerOffset);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        void StartEdit()
        {
            editMode = true;
            posOffset = Vector3.zero;
            eulerOffset = Vector3.zero;
        }

        void ApplyEdit()
        {
            Undo.RegisterCompleteObjectUndo(rigidData, "RigidData");
            if (origin != null)
            {
                for (int i = 0; i < transformedPoints.Length; i++)
                    SetPoint(i, origin.InverseTransformPoint(transformedPoints[i]));
            }
            else
            {
                for (int i = 0; i < transformedPoints.Length; i++)
                    SetPoint(i, transformedPoints[i]);
            }

            posOffset = Vector3.zero;
            eulerOffset = Vector3.zero;

            EditorUtility.SetDirty(rigidData);
        }

        void EndEdit()
        {
            editMode = false;
        }
    }
}

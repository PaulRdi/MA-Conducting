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
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEditor;
using PhaseSpace.Unity;
using PhaseSpace.OWL;

namespace PhaseSpace.Unity
{
    [CustomEditor(typeof(OWLClient))]
    public class OWLClientInspector : Editor
    {
        static bool FConnection = true, FStreaming = true, FVisualization = true, FPacking = false, FRigidbodies = true, FDevices = true;

        SerializedProperty Persistent, AutoScan, AutoOpen, AutoInitialize, ServerAddress, OpenTimeout, StreamingMode, Frequency, SlaveMode, Profile, CalibrationMode, EmbeddedProfile, EmbeddedDevices, FrameEventMask, KeepAlive, OverrideLEDPower, LEDPower, MarkerGizmos, RigidGizmos, CameraGizmos, InitialRigidbodies;
        SerializedProperty PackedMarkers, PackedMarkerVelocities, PackedRigids, PackedRigidVelocities, PackedInputs, PackedDrivers, PackedXBees, PackOnInitialize;
        OWLClient client;
        //public enum ConnectionState { CLOSED, OPENING, OPEN, INITIALIZING, INITIALIZED, NOT_STREAMING, STREAMING }
        void OnEnable()
        {

            client = (OWLClient)target;

            Persistent = serializedObject.FindProperty("Persistent");
            AutoScan = serializedObject.FindProperty("AutoScan");
            AutoOpen = serializedObject.FindProperty("AutoOpen");
            AutoInitialize = serializedObject.FindProperty("AutoInitialize");
            ServerAddress = serializedObject.FindProperty("ServerAddress");
            OpenTimeout = serializedObject.FindProperty("OpenTimeout");
            StreamingMode = serializedObject.FindProperty("StreamingMode");
            Frequency = serializedObject.FindProperty("Frequency");
            SlaveMode = serializedObject.FindProperty("SlaveMode");
            Profile = serializedObject.FindProperty("Profile");
            CalibrationMode = serializedObject.FindProperty("CalibrationMode");
            EmbeddedProfile = serializedObject.FindProperty("EmbeddedProfile");
            EmbeddedDevices = serializedObject.FindProperty("EmbeddedDevices");
            FrameEventMask = serializedObject.FindProperty("FrameEventMask");
            KeepAlive = serializedObject.FindProperty("KeepAlive");
            OverrideLEDPower = serializedObject.FindProperty("OverrideLEDPower");
            LEDPower = serializedObject.FindProperty("LEDPower");
            MarkerGizmos = serializedObject.FindProperty("MarkerGizmos");
            RigidGizmos = serializedObject.FindProperty("RigidGizmos");
            CameraGizmos = serializedObject.FindProperty("CameraGizmos");
            InitialRigidbodies = serializedObject.FindProperty("InitialRigidbodies");

            PackedMarkers = serializedObject.FindProperty("PackedMarkers");
            PackedMarkerVelocities = serializedObject.FindProperty("PackedMarkerVelocities");
            PackedRigids = serializedObject.FindProperty("PackedRigids");
            PackedRigidVelocities = serializedObject.FindProperty("PackedRigidVelocities");
            PackedInputs = serializedObject.FindProperty("PackedInputs");
            PackedDrivers = serializedObject.FindProperty("PackedDrivers");
            PackedXBees = serializedObject.FindProperty("PackedXBees");
            PackOnInitialize = serializedObject.FindProperty("PackOnInitialize");

            EditorApplication.update += Update;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnRigidCreateSceneView;
#else
	    SceneView.onSceneGUIDelegate += OnRigidCreateSceneView;
#endif
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnRigidCreateSceneView;
#else
	    SceneView.onSceneGUIDelegate -= OnRigidCreateSceneView;
#endif
        }

        void Update()
        {
            this.Repaint();
        }

        public override void OnInspectorGUI()
        {
            OWLClient.ConnectionState state = client.State;
            bool closed = state <= OWLClient.ConnectionState.Closed;
            bool open = state == OWLClient.ConnectionState.Open;
            //bool busy = state == OWLClient.ConnectionState.INITIALIZING || state == OWLClient.ConnectionState.OPENING;
            bool playing = EditorApplication.isPlaying;
            bool initialized = state >= OWLClient.ConnectionState.Initialized;
            bool isMaster = client.SlaveMode == PhaseSpace.Unity.SlaveMode.Master;

            EditorGUILayout.LabelField(state.ToString(), EditorStyles.centeredGreyMiniLabel);

            Header("Connection", ref FConnection);
            if (FConnection)
            {
                EditorGUI.BeginDisabledGroup(state > 0);
                {
                    EditorGUILayout.PropertyField(Persistent);
                    EditorGUILayout.PropertyField(AutoScan);

                    EditorGUI.BeginDisabledGroup(AutoScan.boolValue);
                    {
                        ServerAddressField();
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.PropertyField(OpenTimeout);
                    EditorGUILayout.PropertyField(AutoOpen);
                    EditorGUI.BeginDisabledGroup(!AutoOpen.boolValue);
                    {
                        EditorGUILayout.PropertyField(AutoInitialize);
                    }
                    EditorGUI.EndDisabledGroup();

                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!playing);
                {
                    if (state <= OWLClient.ConnectionState.Closed)
                    {
                        if (GUILayout.Button("Open", EditorStyles.toolbarButton))
                            client.Open();
                    }
                    else
                    {
                        if (GUILayout.Button("Close", EditorStyles.toolbarButton))
                            client.Close();

                    }
                }
                EditorGUI.EndDisabledGroup();
            }


            Header("Streaming", ref FStreaming);

            if (FStreaming)
            {
                EditorGUI.BeginDisabledGroup(initialized);
                EditorGUILayout.PropertyField(CalibrationMode);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup((!open && !closed));
                {
                    ProfileField();
                    EditorGUILayout.PropertyField(SlaveMode);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(state == OWLClient.ConnectionState.Initializing);
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(StreamingMode);
                    if (EditorGUI.EndChangeCheck() && state >= OWLClient.ConnectionState.Initialized)

                        client.Context.streaming(StreamingMode.enumValueIndex);


                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(Frequency);
                    if (EditorGUI.EndChangeCheck() && state >= OWLClient.ConnectionState.Initialized)
                        client.Context.frequency(Frequency.intValue);

                    EventMaskButton();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!isMaster);
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(OverrideLEDPower);
                    EditorGUI.BeginDisabledGroup(!OverrideLEDPower.boolValue);
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(LEDPower);
                        EditorGUI.EndDisabledGroup();

                        EditorGUI.BeginDisabledGroup(!client.Ready);
                        {
                            if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(32)))
                            {
                                LEDPowerContext();
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        if (client.State >= OWLClient.ConnectionState.Initialized)
                        {
                            GUILayout.Label(client.GetOption("system.LEDPower"), GUILayout.Width(32));
                        }
                        else
                        {
                            GUILayout.Label("---", GUILayout.Width(32));
                        }

                        GUILayout.EndHorizontal();
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        //update power
                        if (OverrideLEDPower.boolValue)
                            client.SetPower(LEDPower.floatValue);
                    }
                    EditorGUILayout.PropertyField(KeepAlive);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!playing || state < OWLClient.ConnectionState.Open);
                {
                    if (state <= OWLClient.ConnectionState.Open)
                    {
                        if (GUILayout.Button("Initialize", EditorStyles.toolbarButton))
                            client.Initialize();
                    }
                    else
                    {
                        if (GUILayout.Button("Done", EditorStyles.toolbarButton))
                            client.Done();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }


            Visualization();

            Packing();

            Rigidbodies();

            Devices();
            //TODO: 
            //frame number
            //inputs




            serializedObject.ApplyModifiedProperties();
        }

        void Packing()
        {
            Header("Packing", ref FPacking);

            if (FPacking)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PackOnInitialize);
                GUILayout.BeginHorizontal();
                {
                    bool packable = client.State >= OWLClient.ConnectionState.Initialized;

                    bool packing = false;
                    try
                    {
                        packing = packable ? (client.Context.option("pack") == "1" ? true : false) : false;
                    }
                    catch
                    {

                    }



                    EditorGUI.BeginDisabledGroup(!packable);
                    {

                        if (GUILayout.Button("Update PackInfo", EditorStyles.miniButton))
                        {
                            client.UpdatePackInfo();
                        }

                        if (GUILayout.Button(packing ? "Disable" : "Enable", EditorStyles.miniButton))
                        {
                            packing = !packing;
                            if (packing)
                                client.EnablePacking();
                            else
                                client.DisablePacking();
                        }




                    }
                    EditorGUI.EndDisabledGroup();
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(PackedMarkers, true);
                EditorGUILayout.PropertyField(PackedMarkerVelocities, true);
                EditorGUILayout.PropertyField(PackedRigids, true);
                EditorGUILayout.PropertyField(PackedRigidVelocities, true);
                EditorGUILayout.PropertyField(PackedInputs, true);
                EditorGUILayout.PropertyField(PackedDrivers, true);
                EditorGUILayout.PropertyField(PackedXBees, true);



                EditorGUI.indentLevel--;
            }


        }

        void Devices()
        {
            Header("Devices", ref FDevices);

            if (FDevices)
            {
                EditorGUI.BeginDisabledGroup(!(client.State >= OWLClient.ConnectionState.Initialized));
                {
                    DriversList();
                    InputsList();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        void LEDPowerContext()
        {
            float currentPower = float.Parse(client.Context.option("system.LEDPower"));

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Current Power (" + currentPower + ")"), false, HandleLEDPowerContext, currentPower);
            //TODO:
            menu.AddDisabledItem(new GUIContent("Calibrated Power (?)"));
            menu.ShowAsContext();
        }

        void HandleLEDPowerContext(object val)
        {
            float p = (float)val;

            if (client.Ready && OverrideLEDPower.boolValue)
                client.SetPower(p);
            else
            {
                LEDPower.floatValue = p;
                serializedObject.ApplyModifiedProperties();
            }
        }

        Transform createRigidOrigin;
        bool createRigidMode = false;
        uint[] createRigidMarkers;
        Vector3[] createRigidPoints;
        string createRigidName = "rigidTracker";
        int createRigidTrackerId = 1;
        //string omgoptions = "";
        void Rigidbodies()
        {
            Header("Rigidbodies", ref FRigidbodies);

            if (FRigidbodies)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(InitialRigidbodies, true);

                if (client.State < OWLClient.ConnectionState.Initialized)
                {
                    EditorGUI.indentLevel--;
                    return;
                }

                EditorGUI.BeginDisabledGroup(client.SlaveMode == PhaseSpace.Unity.SlaveMode.Slave);
                if (GUILayout.Button(createRigidMode ? "End Rigidbody Creator" : "Start Rigidbody Creator", EditorStyles.toolbarButton))
                {
                    createRigidMode = !createRigidMode;
                }
                EditorGUI.EndDisabledGroup();

                if (createRigidMode)
                {

                    //More buttons!
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Markers", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                    foreach (var m in selectedMarkers)
                    {
                        var marker = client.Markers[(int)m];
                        GUILayout.Label(marker.id.ToString(), marker.Condition > TrackingCondition.Poor ? EditorStyles.miniLabel : PhaseSpaceStyles.MissingMarker, GUILayout.Width(20));
                    }

                    if (selectedMarkers.Count == 0)
                        GUILayout.Label("(None Selected)", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();

                    createRigidOrigin = (Transform)EditorGUILayout.ObjectField("Origin", createRigidOrigin, typeof(Transform), true);

                    GUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(createRigidOrigin == null);
                    {
                        EditorGUILayout.PrefixLabel("Origin Tools");
                        if (rigidOp == RigidOperation.None)
                        {
                            if (GUILayout.Button("Center", EditorStyles.miniButtonLeft))
                                DoRigidOperation(RigidOperation.Center);
                            if (GUILayout.Button("Look At", EditorStyles.miniButtonMid))
                                DoRigidOperation(RigidOperation.LookAt);
                            if (GUILayout.Button("Heading", EditorStyles.miniButtonMid))
                                DoRigidOperation(RigidOperation.Heading);
                            if (GUILayout.Button("Three Point", EditorStyles.miniButtonRight))
                                DoRigidOperation(RigidOperation.ThreePoint);
                        }
                        else
                        {
                            if (GUILayout.Button("Cancel", EditorStyles.miniButton))
                                EndRigidOperation();
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.EndHorizontal();

                    RigidTrackerIdentFields();

                    RigidTrackerButtons();
                }


                EditorGUI.indentLevel--;
                var trackerInfos = client.Context.property<TrackerInfo[]>("trackerinfo").Where(x => x.type == "rigid").OrderBy(x => x.id).ToArray();

                GUILayout.Label("Trackers", EditorStyles.centeredGreyMiniLabel);
                foreach (var t in trackerInfos)
                {
                    var rb = client.Rigids[(int)t.id];
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(t.id.ToString().PadLeft(3, ' ') + "  " + t.name);
                    GUILayout.Label("Condition", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                    Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(48));
                    r.yMin += 4;
                    r.yMax -= 4;
                    EditorGUI.DrawRect(r, Color.grey);
                    r.width = Mathf.RoundToInt(Mathf.InverseLerp(0, 6, rb.cond) * 48);
                    EditorGUI.DrawRect(r, Color.white);

                    EditorGUILayout.ToggleLeft("Kalman", rb.KalmanActive, GUILayout.Width(64));
                    EditorGUILayout.ToggleLeft("Offsets", rb.OffsetsActive, GUILayout.Width(64));

                    if (client.SlaveMode != PhaseSpace.Unity.SlaveMode.Slave)
                    {
                        if (GUILayout.Button("Options", EditorStyles.popup, GUILayout.ExpandWidth(false)))
                        {
                            RigidbodyContext(rb);
                        }
                    }
                    GUILayout.EndHorizontal();

                    /*
                    var ti = client.Context.trackerInfo(rb.id);
                    if (ti != null)
                    {
                        if (ti.options.Contains("maxcond="))
                        {
                            string[] opts = ti.options.Split(' ');
                            for (int i = 0; i < opts.Length; i++)
                            {
                                string[] vals = opts[i].Split('=');
                                if (vals[0] == "maxcond")
                                {
                                    int maxCond = int.Parse(vals[1]);
                                    int previousCond = maxCond;
                                    EditorGUI.BeginChangeCheck();
                                    maxCond = EditorGUILayout.IntSlider("Max Cond", maxCond, 0, 30);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        if (previousCond != maxCond)
                                        {
                                            client.SetTrackerOptions(rb.id, "maxcond=" + maxCond.ToString());
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    */

                    //EditorGUILayout.IntSlider()
                    //Manual options field

                    //GUILayout.BeginHorizontal();
                    //omgoptions = EditorGUILayout.TextField(omgoptions);
                    //if (GUILayout.Button("Set Options"))
                    //{
                    //    client.Context.trackerOptions(t.id, omgoptions);
                    //}
                    //GUILayout.EndHorizontal();
                    //if (ti != null)
                    //    GUILayout.Label(ti.options);

                }
            }


        }

        void RigidTrackerButtons()
        {
            GUILayout.BeginHorizontal();

            //TODO:  Error check this thang
            if (GUILayout.Button("Create Tracker", EditorStyles.miniButtonLeft))
            {
                if (rigidOp != RigidOperation.None)
                    EndRigidOperation();

                bool valid = ValidateCreateRigidTracker();

                if (valid)
                    client.CreateRigidTracker((uint)createRigidTrackerId, createRigidName, createRigidMarkers, createRigidPoints);
            }

            if (GUILayout.Button("Save Asset", EditorStyles.miniButtonRight))
            {
                if (rigidOp != RigidOperation.None)
                    EndRigidOperation();

                bool valid = ValidateCreateRigidTracker();
                if (valid)
                {
                    OWLRigidData data = null;

                    string path = EditorUtility.SaveFilePanelInProject("Save OWLRigidData as...", createRigidName, "asset", "");
                    if (path != "")
                    {
                        var existingObject = AssetDatabase.LoadAssetAtPath<OWLRigidData>(path);
                        if (existingObject != null)
                        {
                            //update
                            data = existingObject;
                        }
                        else
                        {
                            data = CreateInstance<OWLRigidData>();
                            AssetDatabase.CreateAsset(data, path);
                        }

                        data.trackerName = createRigidName;
                        data.trackerId = (uint)createRigidTrackerId;
                        data.ids = createRigidMarkers;
                        data.points = createRigidPoints;

                        EditorUtility.SetDirty(data);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        void RigidTrackerIdentFields()
        {
            createRigidTrackerId = EditorGUILayout.IntField("Tracker ID", createRigidTrackerId);
            createRigidName = EditorGUILayout.TextField("Tracker Name", createRigidName);
        }

        //TODO:  Deal with using Origin Transform instead
        bool ValidateCreateRigidTracker()
        {
            if (createRigidTrackerId <= 0)
                return false;

            if (createRigidName == "")
                return false;

            if (selectedMarkers.Count == 0)
                return false;

            uint[] validMarkers = selectedMarkers.Where(x => client.Markers[(int)x].Condition > TrackingCondition.Poor).ToArray();
            if (validMarkers.Length == selectedMarkers.Count)
            {
                createRigidPoints = new Vector3[validMarkers.Length];
                createRigidMarkers = validMarkers;

                //average position centering
                if (createRigidOrigin == null)
                {
                    Vector3 averagePosition = Vector3.zero;
                    for (int i = 0; i < validMarkers.Length; i++)
                    {
                        Vector3 v = client.Markers[(int)validMarkers[i]].position;
                        v.z *= -1;
                        v /= client.Scale;
                        createRigidPoints[i] = v;
                        averagePosition += v;
                    }


                    averagePosition /= createRigidPoints.Length;

                    for (int i = 0; i < validMarkers.Length; i++)
                    {
                        createRigidPoints[i] -= averagePosition;
                    }
                }
                //scenegraph transform origin
                else
                {
                    Transform temp = new GameObject("lazy").transform;
                    temp.position = createRigidOrigin.position;
                    temp.rotation = createRigidOrigin.rotation;
                    temp.parent = client.transform;
                    temp.localScale = Vector3.one;

                    for (int i = 0; i < validMarkers.Length; i++)
                    {
                        Vector3 v = client.Markers[(int)validMarkers[i]].position;
                        //put in local coordinates
                        v = temp.InverseTransformPoint(v);
                        v.z *= -1;
                        v /= client.Scale;
                        createRigidPoints[i] = v;
                    }

                    Destroy(temp.gameObject);
                }


                return true;
            }
            else
            {
                //error
                Debug.LogWarning("[OWL] Cannot validate Rigid Tracker, some selected markers are not visible!");
            }
            return false;
        }


        //RIGID EDITOR STUFF
        Vector2 dragOrigin;
        Vector2 dragEnd;
        bool dragging;
        List<uint> selectedMarkers = new List<uint>();
        Rect selectBox;
        enum RigidOperation { None, Center, Heading, LookAt, ThreePoint }
        RigidOperation rigidOp;

        Vector3 GetAverageSelectedMarkerPosition()
        {
            Vector3 pos = Vector3.zero;
            int markerCount = 0;
            foreach (var m in selectedMarkers)
            {
                //if (client.Markers[(int)m].Condition > TrackingCondition.Invalid)
                //{
                pos += client.transform.TransformPoint(client.Markers[(int)m].position);
                markerCount++;
                //}
            }

            return pos /= markerCount;
        }

        void DoRigidOperation(RigidOperation op)
        {
            //TODO: Maybe save previous selection set?
            //selectedMarkers.Clear();
            rigidOp = op;

            //Vector3 pos = client.transform.TransformPoint(m.position);
            Vector3 pos = GetAverageSelectedMarkerPosition();

            if (selectedMarkers.Count == 0)
            {
                Debug.LogWarning("No markers selected!");
                EndRigidOperation();
                return;
            }

            if (createRigidOrigin != null)
            {
                switch (rigidOp)
                {
                    case RigidOperation.Center:
                        createRigidOrigin.position = pos;
                        break;
                    case RigidOperation.LookAt:
                        createRigidOrigin.LookAt(pos);
                        break;
                    case RigidOperation.Heading:
                        Quaternion rotation = Quaternion.LookRotation(pos - createRigidOrigin.position);
                        Vector3 fwd = rotation * Vector3.forward;
                        Vector3 right = rotation * Vector3.right;
                        fwd = Quaternion.AngleAxis(-rotation.eulerAngles.z, fwd) * fwd;
                        right = Quaternion.AngleAxis(-rotation.eulerAngles.z, fwd) * right;
                        createRigidOrigin.rotation = Quaternion.LookRotation(Quaternion.AngleAxis(-rotation.eulerAngles.x, right) * fwd);
                        break;
                    case RigidOperation.ThreePoint:
                        if (selectedMarkers.Count != 3)
                        {
                            Debug.LogWarning("Must only select 3 Markers!");
                            break;
                        }

                        Vector3 a = client.transform.TransformPoint(client.Markers[(int)selectedMarkers[0]].position);
                        Vector3 b = client.transform.TransformPoint(client.Markers[(int)selectedMarkers[1]].position);
                        Vector3 c = client.transform.TransformPoint(client.Markers[(int)selectedMarkers[2]].position);
                        UnityEngine.Plane p = new UnityEngine.Plane(a, b, c);
                        Vector3 origin = (a + b + c) / 3;
                        createRigidOrigin.rotation = Quaternion.LookRotation(a - origin, p.normal);
                        break;
                }

                EndRigidOperation();
            }
        }

        void EndRigidOperation()
        {
            rigidOp = RigidOperation.None;
            selectedMarkers.Clear();
        }

        void OnRigidCreateSceneView(SceneView sv)
        {
            if (!createRigidMode)
                return;

            if (client == null || !client.Ready)
                return;

            if (!client.MarkerGizmos)
                OWLEditorUtilities.DrawMarkers(client.Markers, client.transform);

            if (selectedMarkers.Count > 1)
            {
                Vector3 avgPos = GetAverageSelectedMarkerPosition();
                Handles.color = Color.grey;
                Handles.Button(avgPos, Quaternion.identity, 0.0015f, 0, Handles.DotHandleCap);
                foreach (var m in selectedMarkers)
                {
                    Handles.DrawLine(client.transform.TransformPoint(client.Markers[(int)m].position), avgPos);
                }
                //Handles.DrawWireCube(avgPos, Vector3.one * client.Scale * 10);
                //Handles.Label(avgPos, selectedMarkers.Count.ToString());
            }

            foreach (var m in client.Markers)
            {
                if (m.Condition >= TrackingCondition.Normal)
                {
                    Handles.color = OWLEditorUtilities.SlotColors[m.Slot];
                    if (Handles.Button(m.position, Quaternion.identity, selectedMarkers.Contains(m.id) ? 0.005f : 0.0015f, 0.01f, Handles.DotHandleCap))
                    {
                        if (createRigidMode)
                        {
                            if (rigidOp == RigidOperation.None)
                            {
                                if (selectedMarkers.Contains(m.id))
                                    selectedMarkers.Remove(m.id);
                                else
                                    selectedMarkers.Add(m.id);
                            }
                            else
                            {

                            }

                        }
                    }
                }
            }

            var ev = UnityEngine.Event.current;
            if (createRigidMode)
            {
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                if (ev.type == EventType.Layout)
                {
                    HandleUtility.AddDefaultControl(controlId);
                }

                if (ev.type == EventType.KeyUp)
                {
                    if (ev.keyCode == KeyCode.M)
                    {
                        if (selectedMarkers.Count > 0)
                        {
                            ev.Use();
                            Vector3 avgPos = Vector3.zero;
                            Bounds b = new Bounds();
                            foreach (var m in selectedMarkers)
                            {
                                Vector3 pos = client.transform.TransformPoint(client.Markers[(int)m].position);
                                if (b.center == Vector3.zero)
                                    b.center = pos;
                                else
                                    b.Encapsulate(pos);

                                avgPos += pos;
                            }

                            avgPos /= selectedMarkers.Count;

                            //sv.camera.transform.position = avgPos;

                            //sv.camera.transform.Translate(0, 0, selectedMarkers.Count > 1 ? -b.size.magnitude * 2 : -2);
                            sv.LookAtDirect(avgPos, sv.camera.transform.rotation, selectedMarkers.Count > 1 ? b.size.magnitude * 2 : client.Scale * 50);

                        }
                    }


                    //sv.camera.transform.position = 
                }

                if (rigidOp == RigidOperation.None)
                {

                    if (createRigidOrigin != null)
                    {
                        switch (Tools.current)
                        {
                            case Tool.None:
                            case Tool.Rect:
                                OWLEditorUtilities.DrawAxes(createRigidOrigin.position, createRigidOrigin.rotation);
                                break;
                            case Tool.Move:
                                Tools.current = Tool.View;
                                break;
                            case Tool.View:
                                if (Tools.pivotRotation == PivotRotation.Global)
                                    createRigidOrigin.position = Handles.PositionHandle(createRigidOrigin.position, Quaternion.identity);
                                else
                                    createRigidOrigin.position = Handles.PositionHandle(createRigidOrigin.position, createRigidOrigin.rotation);
                                break;
                            case Tool.Rotate:
                                if (Tools.pivotRotation == PivotRotation.Global)
                                    createRigidOrigin.rotation = Handles.RotationHandle(createRigidOrigin.rotation, createRigidOrigin.position);
                                else
                                    createRigidOrigin.localRotation = Handles.RotationHandle(createRigidOrigin.localRotation, createRigidOrigin.position);
                                break;
                        }

                    }


                    switch (ev.type)
                    {
                        case EventType.MouseUp:
                            if (!dragging)
                            {
                                //clear selection unless shift clicking
                                if (!ev.alt && !ev.control && !ev.shift && ev.button == 0 && selectedMarkers.Count > 0)
                                    selectedMarkers.Clear();

                                break;
                            }

                            dragEnd = ev.mousePosition;
                            dragging = false;

                            foreach (var m in client.Markers)
                            {
                                if (m.Condition < TrackingCondition.Normal)
                                    continue;

                                Vector3 point = sv.camera.WorldToScreenPoint(client.transform.TransformPoint(m.position));
                                Vector3 maxDimensions = sv.camera.ViewportToScreenPoint(Vector3.one);
                                point.y = maxDimensions.y - point.y;
                                if (selectBox.Contains(point))
                                {
                                    if (selectedMarkers.Contains(m.id))
                                    {
                                        if (!ev.shift)
                                            selectedMarkers.Remove(m.id);
                                    }
                                    else
                                    {
                                        selectedMarkers.Add(m.id);
                                    }
                                }
                            }

                            break;
                        case EventType.MouseDrag:
                            if (ev.alt || ev.control || ev.button != 0)
                                break;

                            if (!dragging)
                                dragOrigin = ev.mousePosition;

                            dragging = true;
                            dragEnd = ev.mousePosition;
                            ev.Use();
                            break;
                        case EventType.DragExited:
                            dragEnd = ev.mousePosition;
                            dragging = false;
                            break;
                    }

                    if (dragging)
                    {
                        Handles.BeginGUI();
                        float x = Mathf.Min(dragOrigin.x, dragEnd.x);
                        float y = Mathf.Min(dragOrigin.y, dragEnd.y);
                        float w = Mathf.Abs(dragEnd.x - dragOrigin.x);
                        float h = Mathf.Abs(dragEnd.y - dragOrigin.y);
                        selectBox.Set(x, y, w, h);
#if UNITY_2019_1_OR_NEWER
			GUI.backgroundColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
#endif			
			GUI.Box(selectBox, "");
                        Handles.EndGUI();
                    }
                }

                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(0, 0, 512, 128));
                //GUI.Box(new Rect(0, 56, 512, 200), "");
                GUILayout.Label("Rigidbody Creator", PhaseSpaceStyles.GiantSceneViewLabel);

                if (GUILayout.Button("Done", EditorStyles.miniButton, GUILayout.Width(64), GUILayout.Height(32)))
                {
                    createRigidMode = false;
                }
                GUILayout.EndArea();
                Handles.EndGUI();

            }
        }

        void RigidbodyContext(PhaseSpace.Unity.Rigid rb)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Save Asset"), false, HandleRigidbodyCreateAsset, rb);
            //menu.AddDisabledItem(new GUIContent("Create Asset"));
            menu.AddDisabledItem(new GUIContent("Tweak"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Destroy"), false, HandleRigidbodyDestroy, rb);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Read Options"), false, HandleReadOptions, rb);
            //menu.AddItem(new GUIContent("Set MaxCond=1"), false, HandleSetMaxCond1, rb);
            //menu.AddItem(new GUIContent("Set RigidOffsets=1"))
            menu.ShowAsContext();
        }

        void HandleReadOptions(object obj)
        {
            var rb = obj as PhaseSpace.Unity.Rigid;
            Debug.Log(client.Context.trackerInfo(rb.id).options);
        }

        void HandleSetMaxCond1(object obj)
        {
            var rb = obj as PhaseSpace.Unity.Rigid;

            client.SetTrackerOptions(rb.id, "maxcond=1");
            //Debug.Log(client.Context.trackerInfo(rb.id).options);
        }

        void HandleRigidbodyCreateAsset(object obj)
        {
            var rb = obj as PhaseSpace.Unity.Rigid;

            var trackerInfoArr = client.Context.property<TrackerInfo[]>("trackerinfo");
            var info = trackerInfoArr.Where(x => x.id == (int)rb.id).FirstOrDefault();

            if (info == null)
            {
                Debug.LogError("TrackerInfo for Rigidbody ID " + rb.id + " not found!");
                return;
            }
            else
            {
                Debug.Log(info.name + " : " + info.id);
            }
            Vector3[] points = new Vector3[info.marker_ids.Length];
            //Debug.Log("TrackerInfo Marker ID Count: " + points.Length);
            for (int m = 0; m < info.marker_ids.Length; m++)
            {
                var markerId = info.marker_ids[m];
                string opts = client.Context.markerInfo(markerId).options;
                string[] chunks = opts.Split('=');
                for (int i = 0; i < chunks.Length; i++)
                {
                    if (chunks[i] == "pos")
                    {
                        string posStr = chunks[i + 1];
                        string[] vals = posStr.Split(',');
                        points[m] = new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
                    }
                }
            }

            OWLRigidData data = null;

            string path = EditorUtility.SaveFilePanelInProject("Save OWLRigidData as...", info.name, "asset", "");
            if (path != "")
            {
                var existingObject = AssetDatabase.LoadAssetAtPath<OWLRigidData>(path);
                if (existingObject != null)
                {
                    //update
                    data = existingObject;
                }
                else
                {
                    data = CreateInstance<OWLRigidData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.trackerName = info.name;
                data.trackerId = info.id;
                data.ids = info.marker_ids;
                data.points = points;

                EditorUtility.SetDirty(data);
            }
        }
        void HandleRigidbodyDestroy(object obj)
        {
            uint id = (obj as PhaseSpace.Unity.Rigid).id;
            client.DestroyRigidTracker(id);
            //var info = client.Context.trackerInfo(id);
            //uint[] markerIds = info.marker_ids;


            //List<MarkerInfo> markerInfos = new List<MarkerInfo>();
            //foreach (var m in markerIds)
            //{
            //    markerInfos.Add(new MarkerInfo(m, 0, m.ToString()));
            //}
            ////client.Context.destroyTracker(0);
            ////client.Context.createTracker(0, "point", "default");

            //client.Context.assignMarkers(markerInfos.ToArray());

            //client.Context.destroyTracker(id);
        }

        void Visualization()
        {
            Header("Visualization", ref FVisualization);

            if (FVisualization)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(MarkerGizmos);
                EditorGUILayout.PropertyField(RigidGizmos);
                EditorGUILayout.PropertyField(CameraGizmos);
                EditorGUILayout.PrefixLabel("Server Status");
                GUILayout.BeginHorizontal();
                GUILayout.Space(16);
                if (GUILayout.Button("OWLD Log", EditorStyles.miniButtonLeft))
                    Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.OWLDLog((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue));
                if (GUILayout.Button("Packages", EditorStyles.miniButtonMid))
                    Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.Packages((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue));
                if (GUILayout.Button("CPU", EditorStyles.miniButtonMid))
                    Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.CPU((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue));
                if (GUILayout.Button("Storage", EditorStyles.miniButtonMid))
                    Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.Storage((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue));
                if (GUILayout.Button("Network", EditorStyles.miniButtonMid))
                    Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.Network((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue));
                if (GUILayout.Button("Memory", EditorStyles.miniButtonMid))
                    Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.Memory((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue));
                if (GUILayout.Button("NetLink", EditorStyles.miniButtonRight))
                    Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.NetLink((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue));
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

        }

        void SystemStatusContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("OWLD Log"), false, delegate () { Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.OWLDLog((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue)); });
            menu.AddItem(new GUIContent("Packages"), false, delegate () { Debug.Log("[OWL] " + OWLEditorUtilities.ServerStatus.Packages((client.State >= OWLClient.ConnectionState.Opening) ? client.ServerAddress : ServerAddress.stringValue)); });
            menu.ShowAsContext();
        }

        bool driversFoldout = true;
        void DriversList()
        {
            EditorGUI.indentLevel++;
            driversFoldout = EditorGUILayout.Foldout(driversFoldout, "Drivers (" + client.Drivers.Count + ")");
            if (!driversFoldout)
            {
                EditorGUI.indentLevel--;
                return;
            }

            var ordered = client.Drivers.OrderBy(x => x.Value.Info.name).Select(p => p.Value).ToList();

            EditorGUI.indentLevel++;
            foreach (var d in ordered)
            {
                EditorGUI.BeginDisabledGroup(d.Info.status == "" || d.Info.status.StartsWith("removed"));
                GUILayout.BeginHorizontal();
                string deviceName = "Disabled";
                if (client.EmbeddedDevices != null)
                {
                    if (client.EmbeddedDevices.Contains(d))
                    {
                        if (client.EmbeddedDevices[d.Info.hw_id].name != "")
                            deviceName = client.EmbeddedDevices[d.Info.hw_id].name;
                    }
                }
                else
                {
                    deviceName = d.Info.name;
                }

                //TODO:  disable if not using embedded devices
                //EditorGUI.BeginDisabledGroup(client.SlaveMode != PhaseSpace.Unity.SlaveMode.Master);
                if (GUILayout.Button(deviceName, EditorStyles.popup, GUILayout.Width(120)))
                {
                    DriverEnumerationContext(d);
                }
                //EditorGUI.EndDisabledGroup();
                //EditorGUILayout.LabelField("0x" + d.hw_id.ToString("x2") + "\t" + d.status);
                DrawDriverStatus(d);

                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                //TODO:  Parse status tokens
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        void DrawDriverStatus(Driver driver)
        {
            DeviceInfo info = driver.Info;
            bool[] buttonFlags;
            bool[] batteryFlags;
            float signal;
            float capacity;
            string version;
            System.DateTime encoded;

            OWLConversion.GetDriverStatus(info, out buttonFlags, out batteryFlags, out signal, out capacity, out version, out encoded);

            Rect r;

            GUILayout.Label(new GUIContent("0x" + info.hw_id.ToString("x2"), version), GUILayout.Width(90));

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Battery", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            r = EditorGUILayout.GetControlRect(GUILayout.Width(48));
            r.yMin += 4;
            r.yMax -= 4;
            EditorGUI.DrawRect(r, Color.grey);
            r.width = Mathf.RoundToInt(driver.Battery * 48);
            EditorGUI.DrawRect(r, Color.white);
            GUILayout.Space(6);

            r = EditorGUILayout.GetControlRect(GUILayout.Width(8), GUILayout.ExpandWidth(false));
            r.yMin += 4;
            r.yMax -= 4;
            EditorGUI.DrawRect(r, driver.Charging ? Color.red : Color.grey);

            //for (int i = 2; i < 3; i++)
            //{
            //    r = EditorGUILayout.GetControlRect(GUILayout.Width(8), GUILayout.ExpandWidth(false));
            //    r.yMin += 4;
            //    r.yMax -= 4;
            //    EditorGUI.DrawRect(r, batteryFlags[i] ? Color.red : Color.grey);
            //}

            GUILayout.Space(6);
            GUILayout.Label("Signal", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            r = EditorGUILayout.GetControlRect(GUILayout.Width(48));
            r.yMin += 4;
            r.yMax -= 4;
            EditorGUI.DrawRect(r, Color.grey);
            r.width = Mathf.RoundToInt(driver.Signal * 48);
            EditorGUI.DrawRect(r, Color.white);
            GUILayout.Space(6);
            GUILayout.Label("Buttons", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            for (int i = 0; i < 2; i++)
            {
                r = EditorGUILayout.GetControlRect(GUILayout.Width(8), GUILayout.ExpandWidth(false));
                r.yMin += 4;
                r.yMax -= 4;
                EditorGUI.DrawRect(r, driver.Buttons[i] ? Color.red : (driver.Toggles[i] ? Color.blue : Color.grey));
            }
            GUILayout.Space(6);
            GUILayout.Label("Encoded " + encoded.ToShortTimeString(), EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.Label("FW " + version, EditorStyles.miniLabel, GUILayout.Width(128));
            GUILayout.EndHorizontal();

            //EditorGUILayout.LabelField(info.hw_id.ToString("x2") + " " + GetFlags(buttonFlags, "BTNS: ") + " BAT" + capacity.ToString("f2") + GetFlags(batteryFlags, " ") + " SIG" + signal.ToString("f2"));
        }

        string GetFlags(bool[] flags, string header = "Flags: ")
        {
            foreach (var f in flags)
            {
                header += f ? 1 : 0;
            }

            return header;
        }

        void DriverEnumerationContext(Driver driver)
        {
            OWLProfile profile = client.EmbeddedProfile;
            OWLDevices devices = client.EmbeddedDevices;
            GenericMenu menu = new GenericMenu();

            if (profile == null || devices == null)
            {
                //managed by webadmin, allow only force reencode
                menu.AddDisabledItem(new GUIContent("WebAdmin"));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Force Encode"), false, HandleForceEncode, driver.Info);
                menu.ShowAsContext();
                return;
            }


            if (!devices.Contains(driver.Info))
            {
                if (devices.Locked)
                {
                    Debug.LogWarning("[OWL] " + devices.name + " is locked!");
                    EditorGUIUtility.PingObject(devices);
                }
                else
                {
                    Debug.LogWarning("[OWL] Uh oh...");
                }
                return;
            }


            menu.AddDisabledItem(new GUIContent(profile.name));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Disabled"), client.EmbeddedDevices[driver.Info.hw_id].name == "disabled", HandleEnumerateDevice, new object[] { driver.Info, "disabled" });
            menu.AddItem(new GUIContent("Force Encode"), false, HandleForceEncode, driver.Info);
            menu.AddItem(new GUIContent("Calibration"), devices[driver.Info.hw_id].calibration, HandleCalibrationToggle, driver.Info);
            menu.AddSeparator("");
            foreach (var d in profile.devices)
            {
                menu.AddItem(new GUIContent(d.name), driver.Type == Driver.DeviceType.Unknown ? false : driver.Info.name == d.name, HandleEnumerateDevice, new object[] { driver.Info, d.name });
            }

            //Debug.Log("embedded name: " + client.EmbeddedDevices[driver.Info.hw_id].name);
            //Debug.Log("info name: " + driver.Info.name);

            menu.ShowAsContext();
        }

        void HandleCalibrationToggle(object obj)
        {
            var info = (DeviceInfo)obj;
            OWLDevices devices = client.EmbeddedDevices;
            devices[info.hw_id].calibration = !devices[info.hw_id].calibration;
            client.UpdateDevices();
            client.EncodeDevices(info.hw_id);
        }

        void HandleEnumerateDevice(object obj)
        {
            var args = obj as object[];
            var info = args[0] as DeviceInfo;
            var name = args[1] as string;

            client.EmbeddedDevices.Enumerate(info.hw_id, name, true);
            client.UpdateDevices();
            client.EncodeDevices(info.hw_id);
        }

        void HandleForceEncode(object obj)
        {
            var info = obj as DeviceInfo;
            client.EncodeDevices(info.hw_id);
        }

        bool inputsFoldout = true;
        void InputsList()
        {
            EditorGUI.indentLevel++;
            inputsFoldout = EditorGUILayout.Foldout(inputsFoldout, "Inputs (" + client.Inputs.Count + ")");
            if (!inputsFoldout)
            {
                EditorGUI.indentLevel--;
                return;
            }

            var ordered = client.Inputs.OrderBy(x => x.Value.hwid).Select(p => p.Value).ToList();

            EditorGUI.indentLevel++;
            foreach (var i in ordered)
            {
                //EditorGUI.BeginDisabledGroup(d.status == "" || d.status.StartsWith("removed"));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0x" + i.hwid.ToString("x2"), EditorStyles.miniButton, GUILayout.Width(140)))
                {
                    Debug.Log(i);
                }

                if (i.data != null)
                {
                    string data = "";
                    foreach (var b in i.data)
                    {
                        data += b.ToString("x2") + " ";
                    }
                    GUILayout.Label(data, EditorStyles.miniLabel);
                }

                GUILayout.EndHorizontal();
                //EditorGUI.EndDisabledGroup();
                //TODO:  Parse status tokens
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }



        void Header(string label, ref bool foldout)
        {
            GUILayout.Space(10);
            //EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            foldout = EditorGUILayout.Foldout(foldout, label, true, EditorStyles.foldout);
        }

        void ServerAddressField()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(ServerAddress);
                if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(32)))
                {
                    ServerAddressContext();
                }
            }
            GUILayout.EndHorizontal();

        }

        void ServerAddressContext()
        {
            GenericMenu menu = new GenericMenu();
            OWLScan.Active = true;

            EditorUtility.DisplayCancelableProgressBar("Scanning for PhaseSpace servers...", "", 0f);

            for (int i = 0; i < 15; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Scanning for PhaseSpace servers...", "Servers Found: " + OWLScan.Servers.Length.ToString(), i / 15f))
                {
                    break;
                }
                Thread.Sleep(100);
            }

            EditorUtility.ClearProgressBar();
            OWLScan.Active = false;

            foreach (var s in OWLScan.Servers)
            {
                string[] kvs = s.info.Split(' ', '=');
                string hostname = "";

                for (int i = 0; i < kvs.Length; i++)
                {
                    if (kvs[i] == "hostname")
                    {
                        hostname = kvs[i + 1];
                        break;
                    }
                }

                menu.AddItem(new GUIContent(hostname + "/ (use hostname)"), ServerAddress.stringValue == hostname, ServerAddressSelect, hostname);
                menu.AddItem(new GUIContent(hostname + "/" + s.address), ServerAddress.stringValue == s.address, ServerAddressSelect, s.address);

            }

            menu.ShowAsContext();
        }

        void ServerAddressSelect(object s)
        {
            ServerAddress.stringValue = (string)s;
            serializedObject.ApplyModifiedProperties();
        }

        void ProfileField()
        {
            EditorGUI.BeginDisabledGroup(SlaveMode.enumValueIndex != (int)PhaseSpace.Unity.SlaveMode.Master || EmbeddedProfile.objectReferenceValue != null);
            GUILayout.BeginHorizontal();
            {
                if (EmbeddedProfile.objectReferenceValue != null)
                {
                    if (client.Context != null && client.Context.isOpen())
                    {
                        EditorGUILayout.LabelField("Profile", client.Profile);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Profile", ((OWLProfile)EmbeddedProfile.objectReferenceValue).name);
                    }

                }
                else
                {
                    EditorGUILayout.PropertyField(Profile);
                    if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(32)))
                    {
                        ProfileContext();
                    }
                }

            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(SlaveMode.enumValueIndex != (int)PhaseSpace.Unity.SlaveMode.Master);
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(EmbeddedProfile);
            if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                //create new profile
                var newProfile = CreateInstance<OWLProfile>();
                newProfile.slotCount = 4;
                AssetDatabase.CreateAsset(newProfile, "Assets/NewProfile.asset");
                EditorGUIUtility.PingObject(newProfile);
                if (EmbeddedProfile.objectReferenceValue == null)
                {
                    EmbeddedProfile.objectReferenceValue = newProfile;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(EmbeddedDevices);
            if (EmbeddedDevices.objectReferenceValue == null)
            {
                if (GUILayout.Button("Create", EditorStyles.miniButton, GUILayout.Width(64)))
                {
                    var newDevices = CreateInstance<OWLDevices>();
                    AssetDatabase.CreateAsset(newDevices, "Assets/Devices.asset");
                    EditorGUIUtility.PingObject(newDevices);
                    EmbeddedDevices.objectReferenceValue = newDevices;
                }
            }
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (EmbeddedProfile.objectReferenceValue != null)
                {
                    //set value?
                    //Profile.stringValue = ((OWLProfile)EmbeddedProfile.objectReferenceValue).name;
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        void ProfileContext()
        {
            GenericMenu menu = new GenericMenu();

            if (client.State <= OWLClient.ConnectionState.Closed)
            {
                PopulateProfiles(menu, RetrieveProfiles());
            }
            else if (client.State >= OWLClient.ConnectionState.Open)
            {
                PopulateProfiles(menu, client.Profiles);
            }

            menu.ShowAsContext();
        }

        void PopulateProfiles(GenericMenu menu, string[] profiles)
        {
            foreach (var p in profiles)
            {
                menu.AddItem(new GUIContent(p), p == Profile.stringValue, ProfileSelect, p);
            }
        }

        string[] RetrieveProfiles()
        {
            Context context = new Context();

            int ret = context.open(ServerAddress.stringValue, "timeout=200000");
            if (ret != 1)
            {
                EditorUtility.DisplayDialog("[OWL] Error", "Connection timed out!\r\n" + ServerAddress.stringValue, "Ok");
                return new string[0];
            }

            //133rc10
            //string[] profiles = context.property<string>("profiles").Split(',');
            //133rc6
            string[] profiles = context.property<string[]>("profiles");

            context.close();

            return profiles;
        }

        void ProfileSelect(object p)
        {
            Profile.stringValue = (string)p;
            serializedObject.ApplyModifiedProperties();
        }

        void EventMaskButton()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Frame Event Mask");
                string str = "";
                for (int i = 0; i < 15; i++)
                {
                    int n = 1 << i;
                    if ((FrameEventMask.intValue & n) > 0)
                    {
                        str += ((FrameEventType)n).ToString() + ", ";
                    }
                }

                if (str == "")
                    str = "None";
                else
                    str = str.TrimEnd(',', ' ');

                if (GUILayout.Button(str, EditorStyles.popup))
                    EventMaskContext();

            }
            GUILayout.EndHorizontal();
        }

        void EventMaskContext()
        {
            GenericMenu menu = new GenericMenu();

            for (int i = 0; i < 15; i++)
            {
                int n = 1 << i;

                menu.AddItem(new GUIContent(((FrameEventType)n).ToString()), (FrameEventMask.intValue & n) > 0, EventMaskSelect, n);
            }

            menu.ShowAsContext();
        }

        void EventMaskSelect(object n)
        {
            int val = FrameEventMask.intValue;

            val = val ^ (int)n;

            FrameEventMask.intValue = val;
            serializedObject.ApplyModifiedProperties();

            if (client.State >= OWLClient.ConnectionState.Initialized)
            {
                string optStr = "";
                client.GetFrameEventMaskOptions(ref optStr);
                client.SetOption(optStr);
            }
        }
    }
}

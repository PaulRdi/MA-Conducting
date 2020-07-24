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
using UnityEditor.AnimatedValues;
using System;
using System.Linq;
namespace PhaseSpace.Unity
{
    [CustomEditor(typeof(OWLProfile))]
    public class OWLProfileInspector : Editor
    {
        static OWLProfile lastProfile;
        static List<AnimBool> openDevices = new List<AnimBool>();

        public static string SanitizeName(OWLProfile profile, string name)
        {
            //split by dash, find last dash

            string[] chunks = name.Split('-');

            int num = 1;

            if (chunks.Length == 1)
            {
                return name + "-" + num;
            }

            string combine = chunks[0];
            //ignore last chunk
            for (int i = 1; i < chunks.Length - 1; i++)
            {
                combine += "-" + chunks[i];
            }

            if (int.TryParse(chunks.Last(), out num))
            {
                string n = combine + "-" + num;
                while (profile[n] != null)
                {
                    num++;
                    n = combine + "-" + num;
                }
            }

            return combine + "-" + num;
        }

        OWLProfile profile;

        void Export()
        {
            string json = profile.GetJSON();

            string path = EditorUtility.SaveFilePanel("Save JSON as...", "", profile.name, "json");
            if (path != "")
            {
                System.IO.File.WriteAllText(path, json);
            }
        }

        void OnEnable()
        {
            profile = (OWLProfile)target;

            if (profile != lastProfile || profile.devices.Count != openDevices.Count)
            {
                openDevices.Clear();
                for (int i = 0; i < profile.devices.Count; i++)
                {
                    //openDevices.Add(new AnimBool(false));
                    //openDevices[i].valueChanged.AddListener(Repaint);
                }
            }
            else
            {
                for (int i = 0; i < openDevices.Count; i++)
                {
                    //openDevices[i].valueChanged.AddListener(Repaint);
                }
            }

            lastProfile = profile;
        }

        void OnDisable()
        {
            for (int i = 0; i < openDevices.Count; i++)
            {
                openDevices[i].valueChanged.RemoveAllListeners();
            }
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Export", EditorStyles.miniButton))
            {
                Export();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(profile.name, EditorStyles.boldLabel);
            GUILayout.Space(20);
            DrawSlotSelector();
            DrawSlotList();
            GUILayout.Space(20);
            DrawAddDevice();
            GUILayout.Space(20);
            DrawDeviceList();

            //TODO: Reduce repaints for mouse over
            Repaint();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(profile);
            }
            //base.OnInspectorGUI();
        }

        void DrawSlotSelector()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Slot Count", GUILayout.Width(80));
                if (GUILayout.Button(profile.slotCount.ToString(), EditorStyles.popup, GUILayout.Width(36)))
                {
                    SlotSelectContext();
                }
            }

            GUILayout.EndHorizontal();
        }

        void SlotSelectContext()
        {
            GenericMenu menu = new GenericMenu();

            for (int i = 1; i < 13; i++)
            {
                menu.AddItem(new GUIContent(i.ToString() + " (" + i * 64 + ")"), profile.slotCount == i, HandleSlotSelect, i);
            }

            menu.ShowAsContext();
        }

        void HandleSlotSelect(object args)
        {
            int s = (int)args;
            profile.slotCount = s;
        }

        void DrawSlotList()
        {
            EditorGUILayout.LabelField("Slot Allocation");

            int[] slotMarkers = profile.MarkersUsedPerSlot;
            GUILayout.BeginHorizontal();
            for (int i = 0; i < profile.slotCount; i++)
            {
                EditorGUILayout.LabelField("<b><color=" + PhaseSpaceStyles.SlotColorStrings[i] + ">" + i + "</color></b>\n" + (64 - slotMarkers[i]), PhaseSpaceStyles.SlotLabel, GUILayout.Width(32));
            }
            GUILayout.EndHorizontal();
        }

        void DrawDeviceList()
        {

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Devices", GUILayout.Width(70));
            DrawSortButton();
            GUILayout.EndHorizontal();
            //TODO: Other device types
            for (int d = 0; d < profile.devices.Count; d++)
            {
                if (openDevices.Count - 1 < d)
                    openDevices.Add(new AnimBool(false));

                switch (profile.devices[d].type)
                {
                    case OWLProfile.Device.Type.Microdriver:
                        MicrodriverInspector.Draw(profile, d, openDevices[d]);
                        break;

                }
                GUILayout.Space(10);
            }
        }

        void DrawAddDevice()
        {

            EditorGUILayout.LabelField("Create Device", GUILayout.Width(120));
            GUILayout.BeginHorizontal(GUILayout.Width(200));
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Hub", EditorStyles.miniButtonLeft);
                GUILayout.Button("Driver", EditorStyles.miniButtonMid);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Microdriver", EditorStyles.miniButtonRight))
                {
                    AddMicrodriver();
                }
            }
            GUILayout.EndHorizontal();
        }

        void DrawSortButton()
        {
            //TODO: multiple sorting options
            if (GUILayout.Button("Sort", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                profile.devices.Sort();
                EditorUtility.SetDirty(profile);
            }
        }

        void AddMicrodriver()
        {
            var device = new OWLProfile.Device("dev-0", OWLProfile.Device.Type.Microdriver);
            if (profile[device.name] != null)
                device.name = SanitizeName(profile, device.name);

            profile.devices.Add(device);
            for (int i = 0; i < 8; i++)
            {
                device.strings[0].leds[i].active = true;
            }

            MicrodriverInspector.AutoAssignMarkerIds(profile, profile.devices.Count - 1);
            MicrodriverInspector.AutoAssignSlots(profile, profile.devices.Count - 1);


        }
    }

    static class MicrodriverInspector
    {
        static int[,] slotTable = new int[4, 4];
        static GUIStyle miniToggleStyle;
        static int slotCount;
        static Color badColor;
        static int[] pinMap = new int[16] { 1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14 };

        static MicrodriverInspector()
        {
            slotTable = new int[4, 4];
            miniToggleStyle = new GUIStyle(EditorStyles.miniLabel);
            miniToggleStyle.fontSize = 8;
            badColor = Color.yellow;
            //miniToggleStyle.fontStyle = FontStyle.Bold;

        }

        static void ContextMenu(OWLProfile profile, int deviceIndex)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate"), false, HandleContextDuplicate, new object[] { profile, deviceIndex });
            menu.AddItem(new GUIContent("Auto Marker IDs"), false, HandleAutoAssignMarkers, new object[] { profile, deviceIndex });
            menu.AddItem(new GUIContent("Auto Slots"), false, HandleAutoSlots, new object[] { profile, deviceIndex });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, HandleContextDelete, new object[] { profile, deviceIndex });
            menu.ShowAsContext();
        }

        static void HandleContextDelete(object obj)
        {
            object[] arr = (object[])obj;
            OWLProfile profile = (OWLProfile)arr[0];
            int index = (int)arr[1];

            profile.devices.RemoveAt(index);
        }

        static void HandleContextDuplicate(object obj)
        {
            object[] arr = (object[])obj;
            OWLProfile profile = (OWLProfile)arr[0];
            int index = (int)arr[1];
            var d = new OWLProfile.Device(profile.devices[index]);

            d.name = OWLProfileInspector.SanitizeName(profile, d.name);
            profile.devices.Add(d);
            AutoAssignMarkerIds(profile, profile.devices.Count - 1);
        }



        static void HandleAutoAssignMarkers(object obj)
        {
            object[] arr = (object[])obj;
            OWLProfile profile = (OWLProfile)arr[0];
            int index = (int)arr[1];

            AutoAssignMarkerIds(profile, index);
        }

        static void HandleAutoSlots(object obj)
        {
            object[] arr = (object[])obj;
            OWLProfile profile = (OWLProfile)arr[0];
            int index = (int)arr[1];

            AutoAssignSlots(profile, index);
        }

        public static void AutoAssignMarkerIds(OWLProfile profile, int deviceIndex)
        {
            var device = profile.devices[deviceIndex];

            var str = device.strings[0];

            for (int i = 0; i < str.leds.Count; i++)
            {
                if (str.leds[i].active)
                {
                    str.leds[i].id = -1;
                }
            }

            for (int i = 0; i < str.leds.Count; i++)
            {
                int n = pinMap[i];
                if (str.leds[n].active)
                {
                    str.leds[n].id = profile.GetNextAvailableID();
                }
            }
        }

        public static void AutoAssignSlots(OWLProfile profile, int deviceIndex)
        {
            var device = profile.devices[deviceIndex];
            List<int> anodes = new List<int>();
            List<List<int>> anodeSlots = new List<List<int>>();
            List<int> anodeLEDs = new List<int>();
            for (int i = 0; i < device.strings[0].leds.Count; i++)
            {
                int n = pinMap[i];
                var led = device.strings[0].leds[n];
                int anode = i / 4;

                if (led.active)
                {
                    if (!anodes.Contains(anode))
                    {
                        anodes.Add(anode);
                        anodeSlots.Add(new List<int>());
                        anodeLEDs.Add(0);
                    }

                    anodeLEDs[anodes.Count - 1]++;
                    //i = (anode + 1) * 4;
                    continue;
                }
            }

            if (anodes.Count == 0)
                return;

            List<int> viableSlots = new List<int>();
            List<int> viableSlotCounts = new List<int>();
            int[] slotTable = profile.MarkersUsedPerSlot;
            for (int i = 0; i < slotTable.Length; i++)
            {
                if (slotTable[i] < 64)
                {
                    viableSlots.Add(i);
                    viableSlotCounts.Add(64 - slotTable[i]);
                }

            }



            int ac = 0;
            int acBreakout = 0;
            while (viableSlots.Count > 0)
            {
                if (anodeSlots[ac].Count == anodeLEDs[ac])
                {
                    acBreakout++;
                }
                else
                {
                    acBreakout = 0;
                    anodeSlots[ac].Add(viableSlots[0]);
                    viableSlots.RemoveAt(0);
                }

                ac++;
                if (ac == anodes.Count)
                    ac = 0;

                if (acBreakout == 4)
                {
                    break;
                }

            }

            for (int i = 0; i < anodeSlots.Count; i++)
            {
                int anode = anodes[i];
                int offset = anode * 4;
                int sc = 0;
                for (int l = offset; l < offset + 4; l++)
                {
                    int n = pinMap[l];
                    var led = device.strings[0].leds[n];
                    if (led.active == false)
                        continue;

                    //Debug.Log(anodeSlots.Count + " : " + anodeSlots[anode].Count);
                    try
                    {
                        if (anodeSlots[i].Count == 0)
                        {
                            //no remaining slots!
                            led.slot = -1;
                        }
                        else
                        {
                            led.slot = anodeSlots[i][sc];
                            viableSlotCounts[led.slot]--;

                            //TODO: Prevent this 
                            if (viableSlotCounts[led.slot] < 0)
                            {
                                Debug.Log("WARNING!");
                            }
                        }
                    }
                    catch
                    {
                        Debug.Log(i.ToString() + " : " + sc);
                    }


                    sc++;
                    if (sc >= anodeSlots[i].Count)
                        sc = 0;
                }
            }
        }

        public static void Draw(OWLProfile profile, int deviceIndex, AnimBool open)
        {
            var device = profile.devices[deviceIndex];

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(device.name, PhaseSpaceStyles.Microdriver))
            {
                if (Event.current.button == 0)
                    open.target = !open.target;
                else if (Event.current.button == 1)
                {
                    //context menu
                    ContextMenu(profile, deviceIndex);
                }
            }


            for (int a = 0, n = 0; a < 4; a++)
            {
                GUILayout.Space(10);
                for (int c = 0; c < 4; c++, n++)
                {
                    var led = device.strings[0].leds[pinMap[n]];
                    string label = led.active ? led.id.ToString() : "";
                    if (open.target && led.active && led.slot >= 0)
                        label += "\n<color=" + PhaseSpaceStyles.SlotColorStrings[led.slot] + ">" + led.slot + "</color>";
                    //label += "<color=xff00ff>9</color>";
                    //GUIContent content = new GUIContent(label, "Slot " + led.slot);
                    //EditorGUILayout.LabelField(label, led.active ? PhaseSpaceStyles.LEDOn : PhaseSpaceStyles.LEDOff, GUILayout.Width(20));
                    if (GUILayout.Button(label, led.active ? PhaseSpaceStyles.LEDOn : PhaseSpaceStyles.LEDOff, GUILayout.Width(20)))
                    {
                        if (Event.current.button == 1)
                        {
                            //toggle LED
                            led.active = !led.active;
                        }
                    }

                }
            }
            GUILayout.EndHorizontal();
            //GUILayout.Space(40);

            //GUILayout.BeginHorizontal(GUILayout.Width(250));
            //GUILayout.Button("DEL", EditorStyles.miniButtonLeft, GUILayout.Width(36));
            //GUILayout.Button("DUP", EditorStyles.miniButtonRight, GUILayout.Width(36));
            //GUILayout.Space(10);
            //open.target = EditorGUILayout.Foldout(open.target, device.name + "\t\t(" + device.LEDCount + " Markers)", true);
            //GUILayout.EndHorizontal();

            //if (!open.value)
            //    return;

            if (EditorGUILayout.BeginFadeGroup(open.faded))
            {
                UpdateSlotUseage(profile, device);
                GUILayout.BeginHorizontal();
                device.name = EditorGUILayout.TextField(device.name, GUILayout.Width(134));
                GUILayout.Label("Encoded Power", GUILayout.Width(100));
                device.encodedPower = EditorGUILayout.Slider(device.encodedPower, 0, 1);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                //EditorGUILayout.Space();
                EditorGUILayout.LabelField("Anodes", GUILayout.Width(48));
                GUILayout.Space(48);
                GUILayout.Label("Slot       ID", GUILayout.Width(70));
                GUILayout.Space(78);
                GUILayout.Label("Slot       ID", GUILayout.Width(70));
                GUILayout.Space(78);
                GUILayout.Label("Slot       ID", GUILayout.Width(70));
                GUILayout.Space(78);
                GUILayout.Label("Slot       ID", GUILayout.Width(70));

                GUILayout.EndHorizontal();
                for (int i = 0; i < 4; i++)
                {
                    DrawAnode(profile, device, i);
                }
            }
            EditorGUILayout.EndFadeGroup();


        }

        static void UpdateSlotUseage(OWLProfile profile, OWLProfile.Device device)
        {
            slotCount = profile.slotCount;
            var leds = device.strings[0].leds;
            int index = 0;
            for (int a = 0; a < 4; a++)
            {
                for (int c = 0; c < 4; c++)
                {
                    slotTable[a, c] = leds[index].active ? leds[index].slot : -1;
                    index++;
                }
            }
        }

        static void DrawAnode(OWLProfile profile, OWLProfile.Device device, int index)
        {
            var leds = device.strings[0].leds;


            EditorGUI.indentLevel++;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("A" + index, GUILayout.Width(48));
            for (int i = index * 4; i < (index * 4) + 4; i++)
            {
                int n = pinMap[i];
                OWLProfile.Device.String.LED led = leds[n];
                led.active = EditorGUILayout.ToggleLeft(n.ToString(), led.active, miniToggleStyle, GUILayout.Width(44));

                EditorGUI.BeginDisabledGroup(!led.active);
                {
                    if (led.active)
                    {
                        if (led.slot == -1 || led.slot >= profile.slotCount)
                            GUI.color = badColor;
                        else if (IsSlotValid(index, led.slot))
                            GUI.color = Color.white;
                        else
                            GUI.color = badColor;
                    }

                    if (GUILayout.Button(led.slot == -1 ? "-" : led.slot.ToString(), EditorStyles.popup, GUILayout.Width(32)))
                    {
                        SlotContextMenu(led);
                    }

                    GUI.color = Color.white;

                    if (led.active && profile.IsMarkerIdDuplicated(led.id))
                        GUI.color = badColor;
                    else
                        GUI.color = Color.white;

                    //TODO:  auto button
                    EditorGUI.BeginChangeCheck();
                    led.id = EditorGUILayout.IntField(led.id, GUILayout.Width(64));

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (led.id < 0)
                            led.id = 0;
                        else if (led.id > profile.slotCount * 64)
                            led.id = profile.slotCount * 64;
                    }



                    GUI.color = Color.white;
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        static void SlotContextMenu(OWLProfile.Device.String.LED led)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Undefined"), led.slot == -1, HandleSlotContextMenu, new object[2] { led, -1 });
            for (int i = 0; i < slotCount; i++)
            {
                bool isValid = IsSlotValid(led.address / 4, i);

                if (isValid)
                    menu.AddItem(new GUIContent(i.ToString()), led.slot == i, HandleSlotContextMenu, new object[2] { led, i });
                else
                    menu.AddDisabledItem(new GUIContent(i.ToString() + " (A" + SlotUsedBy(i) + ")"));
            }

            menu.ShowAsContext();
        }

        static void HandleSlotContextMenu(object data)
        {
            object[] arr = (object[])data;
            var led = (OWLProfile.Device.String.LED)arr[0];
            led.slot = (int)arr[1];
        }

        static bool IsSlotValid(int anode, int slot)
        {
            for (int a = 0; a < 4; a++)
            {
                if (a == anode)
                    continue;

                for (int c = 0; c < 4; c++)
                {
                    if (slotTable[a, c] == slot)
                        return false;
                }
            }
            return true;
        }

        static int SlotUsedBy(int slot)
        {
            for (int a = 0; a < 4; a++)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (slotTable[a, c] == slot)
                        return a;
                }
            }
            return -1;
        }
    }
}
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
using System.IO;
using System.Linq;

using UnityEngine;
using MiniJSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PhaseSpace.Unity
{


    [CreateAssetMenu(fileName = "Devices", menuName = "PhaseSpace/OWL Devices", order = 1)]
    public class OWLDevices : ScriptableObject
    {

        [ContextMenu("Dump JSON")]
        void DumpJSON()
        {
            Debug.Log(GetJSON());
        }

        [System.Serializable]
        public class Device
        {
            [HWID]
            public ulong hwid;
            public string name;
            public OWLProfile.Device.Type type;
            public bool calibration;

            public Device(string name, OWLProfile.Device.Type type, ulong hwid, bool calibration = false)
            {
                this.name = name;
                this.type = type;
                this.hwid = hwid;
                this.calibration = calibration;
            }

            public Device(string name, string type, ulong hwid, bool calibration = false)
            {
                this.name = name;
                this.hwid = hwid;
                this.calibration = calibration;

                switch (type)
                {
                    case "microdriver":
                        this.type = OWLProfile.Device.Type.Microdriver;
                        break;
                    case "driver":
                        this.type = OWLProfile.Device.Type.Driver;
                        break;
                    case "hub":
                        this.type = OWLProfile.Device.Type.Hub;
                        break;
                }
            }
        }

        public List<Device> Devices = new List<Device>();
        public bool Locked = false;

        [System.NonSerialized]
        public bool dirty = false;

        public Device this[ulong hwid]
        {
            get
            {
                if (!Contains(hwid))
                    return null;

                return Devices.Where(x => x.hwid == hwid).FirstOrDefault();
            }
        }

        public bool Enumerate(ulong hwid, string name, bool serialize = false)
        {
            if (Contains(hwid))
            {
                this[hwid].name = name;
                dirty = true;
                if (serialize)
                {
#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#else
                    //TODO:  Save to JSON somewhere
#endif
                }

                return true;
            }
            return false;
        }

        public bool AddDevice(PhaseSpace.OWL.DeviceInfo info, bool serialize = false)
        {
            if (Locked)
                return false;

            //string hwid = "0x" + info.hw_id.ToString("x2");
            if (Contains(info.hw_id))
                return false;

            string name = "";

            Devices.Add(new Device(name, info.type, info.hw_id, false));

            if (serialize)
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#else
                //TODO:  Save to JSON somewhere
#endif
            }


            dirty = true;

            return true;
        }


        public void ApplyChanges()
        {
            if (dirty)
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#else
            //TODO: auto serialize to JSON
#endif

                dirty = false;
            }
        }


        public bool Contains(ulong hwid)
        {
            if (Devices.Where(x => x.hwid == hwid).Count() > 0)
                return true;

            return false;
        }

        public bool Contains(PhaseSpace.OWL.DeviceInfo info)
        {
            return Contains(info.hw_id);
        }

        public bool Contains(PhaseSpace.Unity.Driver driver)
        {
            return Contains(driver.Info);
        }

        public string GetJSON()
        {
            Hashtable root = new Hashtable();

            List<Hashtable> deviceEntries = new List<Hashtable>();
            foreach (var d in Devices)
            {
                Hashtable table = new Hashtable();
                table.Add("name", d.name);
                switch (d.type)
                {
                    case OWLProfile.Device.Type.Microdriver:
                        table.Add("type", "microdriver");
                        break;
                    case OWLProfile.Device.Type.Driver:
                        table.Add("type", "driver");
                        break;
                    case OWLProfile.Device.Type.Hub:
                        table.Add("type", "hub");
                        break;

                }
                table.Add("hwid", d.hwid);
                table.Add("id", "0");
                if (d.calibration)
                    table.Add("calibration", "1");
                deviceEntries.Add(table);
            }

            root.Add("devices", deviceEntries);

            return Json.Serialize(root);
        }

        public void ParseJSON(Dictionary<string, object> root)
        {
            List<object> jsonDevices = (List<object>)root["devices"];

            Devices.Clear();

            foreach (var obj in jsonDevices)
            {
                var table = (Dictionary<string, object>)obj;

                string name = table["name"] as string;
                string hwidStr = table["hwid"] as string;
                string type = table["type"] as string;


                Devices.Add(new Device(name, type, ulong.Parse(hwidStr, System.Globalization.NumberStyles.HexNumber)));
            }
        }

        [ContextMenu("Load")]
        public void Load()
        {
            if (Locked)
                return;

            string path = Path.Combine(Application.dataPath, "devices.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning(path + " Not Found!");
                return;
            }

            string json = File.ReadAllText(path);
            Dictionary<string, object> root = (Dictionary<string, object>)Json.Deserialize(json);
            ParseJSON(root);
        }

        [ContextMenu("Save")]
        public void Save()
        {
            string path = Path.Combine(Application.dataPath, "devices.json");

            File.WriteAllText(path, GetJSON());
        }

        public string GetWarnings()
        {
            string warning = "";
            Dictionary<ulong, int> idCount = new Dictionary<ulong, int>();
            foreach (var d in Devices)
            {
                if (!idCount.ContainsKey(d.hwid))
                    idCount[d.hwid] = 0;

                idCount[d.hwid]++;
                if(idCount[d.hwid] > 1)
                {
                    warning += "Duplicate HWID(0x" + d.hwid.ToString("x4") + ")";
                }
            }

            return warning;
        }
    }
}
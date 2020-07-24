using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProfile", menuName = "PhaseSpace/OWL Profile", order = 1)]
public class OWLProfile : ScriptableObject
{
    [System.Serializable]
    public class Device : System.IComparable<Device>
    {
        [System.Serializable]
        public class String
        {
            [System.Serializable]
            public class LED
            {
                public int id;
                public string type;
                public int slot;
                public int address;
                //editor param
                public bool active;

                public LED()
                {

                }

                public LED(LED src)
                {
                    id = src.id;
                    type = src.type;
                    slot = src.slot;
                    address = src.address;
                    active = src.active;
                }
            }

            public string name = "str-1";
            public int id = 100;
            public List<LED> leds = new List<LED>();

            public String(string name, int id, int ledCount = 0)
            {
                this.name = name;
                this.id = id;
                leds = new List<LED>(new LED[ledCount]);
                for (int i = 0; i < leds.Count; i++)
                {
                    leds[i] = new LED();
                    leds[i].slot = -1;
                }
            }

            public String(String src)
            {
                name = src.name;
                id = src.id;
                leds = new List<LED>();
                for (int i = 0; i < src.leds.Count; i++)
                {
                    leds.Add(new LED(src.leds[i]));
                }
            }
        }

        public enum Type { Microdriver, Driver, Hub }

        public string name;
        public Type type;

        public List<String> strings;

        public float encodedPower = 0.3f;

        public Device(string name, Type type)
        {
            this.name = name;
            this.type = type;
            strings = new List<String>();

            if (type == Type.Microdriver)
            {
                var str = new String("str-1", 100, 16);
                int addr = 0;
                for (int a = 0; a < 4; a++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        str.leds[addr].address = addr;
                        addr++;
                    }
                }
                strings.Add(str);
            }
        }

        public Device(Device src)
        {
            name = src.name;
            type = src.type;
            strings = new List<String>();
            for (int i = 0; i < src.strings.Count; i++)
            {
                strings.Add(new String(src.strings[i]));
            }
        }

        public int LEDCount
        {
            get
            {
                int count = 0;
                foreach (var s in strings)
                {
                    foreach (var l in s.leds)
                    {
                        if (l.active)
                            count++;
                    }
                }

                return count;
            }
        }

        int IComparable<Device>.CompareTo(Device other)
        {
            return name.CompareTo(other.name);
        }
    }

    public int profileID = 1;
    public int configID = 1;
    public string date = "Today";
    public string description = "N/A";
    public int bitCount = 8;
    public int slotCount = 4;
    public int configSelect = 0;
    public List<Device> devices = new List<Device>();

    public Device this[int index]
    {
        get
        {
            return devices[index];
        }
    }

    public Device this[string name]
    {
        get
        {
            return devices.Where(x => x.name == name).Any() ? devices.Where(x => x.name == name).First() : null;
        }
    }
    //TODO:  Cache?
    public bool IsMarkerIdUsed(int id)
    {
        foreach (var d in devices)
        {
            foreach (var s in d.strings)
            {
                foreach (var l in s.leds)
                {
                    if (l.active && l.id == id)
                        return true;
                }
            }
        }
        return false;
    }

    public bool IsMarkerIdDuplicated(int id)
    {
        int count = 0;
        foreach (var d in devices)
        {
            foreach (var s in d.strings)
            {
                foreach (var l in s.leds)
                {
                    if (l.active && l.id == id)
                    {
                        count++;
                        if (count > 1)
                            return true;
                    }

                }
            }
        }
        return false;
    }

    public int GetNextAvailableID()
    {
        for (int i = 0; i < 64 * slotCount; i++)
        {
            if (IsMarkerIdUsed(i))
                continue;

            return i;
        }

        return -1;
    }

    public int[] MarkersUsedPerSlot
    {
        get
        {
            int[] slots = new int[this.slotCount];
            foreach (var d in devices)
            {
                foreach (var s in d.strings)
                {
                    foreach (var l in s.leds)
                    {
                        if (l.active)
                        {
                            if (l.slot < 0 || l.slot >= slotCount)
                                continue;
                            slots[l.slot]++;
                        }

                    }
                }
            }

            return slots;
        }
    }

    public string GetJSON()
    {
        Hashtable root = new Hashtable();

        root.Add("name", name);
        root.Add("profileID", profileID);
        root.Add("configID", configID);
        root.Add("configSelect", 0);
        root.Add("date", date);
        root.Add("description", description);
        root.Add("bitCount", bitCount.ToString());
        root.Add("slotCount", slotCount.ToString());

        List<object> devices = new List<object>();
        for (int i = 0; i < this.devices.Count; i++)
        {
            var d = this.devices[i];
            Hashtable device = new Hashtable();
            device.Add("name", d.name);
            device.Add("type", d.type.ToString().ToLower());

            //DEPRECATED in 5.2
            //Set ID to 0 to enable Name Matching
            //device.Add("id", 0);

            //strings
            List<object> strings = new List<object>();
            foreach (var s in d.strings)
            {
                Hashtable str = new Hashtable();

                str.Add("name", s.name);
                str.Add("id", s.id);

                List<object> leds = new List<object>();
                int c = 0;
                foreach (var l in s.leds)
                {
                    //invalid LED
                    if (!l.active || l.slot < 0 || l.id < 0)
                        continue;

                    Hashtable led = new Hashtable();
                    led.Add("slot", l.slot.ToString());
                    led.Add("id", l.id);
                    if (l.type != "" && l.type != null)
                    {
                        led.Add("type", l.type);
                    }
                    else
                    {
                        //deprecated
                        //led.Add("type", ((char)(0x41 + c)).ToString());
                        led.Add("type", "A");
                        led.Add("address", l.address);
                    }
                    leds.Add(led);
                    c++;
                }
                str.Add("leds", leds);

                strings.Add(str);
            }

            device.Add("strings", strings);
            device.Add("power", Mathf.Clamp01(d.encodedPower).ToString("f2"));

            devices.Add(device);
        }

        root.Add("devices", devices);

        return MiniJSON.Json.Serialize(root);
    }
}
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
using PhaseSpace.OWL;

namespace PhaseSpace.Unity
{

    /// <summary>
    /// Useful conversion classes
    /// </summary>
    public static class OWLConversion
    {
        /// <summary>
        /// Converts a Position Rotation into a PhaseSpace Pose.
        /// Optionally performs Unity left-handed negation
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static float[] Pose(Vector3 position, Quaternion rotation, bool raw = false)
        {
            if (!raw)
                return new float[7] { position.x, position.y, -position.z, rotation.w, -rotation.x, -rotation.y, rotation.z };
            else
                return new float[7] { position.x, position.y, position.z, rotation.w, rotation.x, rotation.y, rotation.z };
        }

        /// <summary>
        /// Gets Vector3 Position from a PhaseSpace Pose.
        /// Optionally performs Unity left-handed negation
        /// </summary>
        /// <param name="p"></param>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static Vector3 PosePosition(float[] p, bool raw = false)
        {
            if (!raw)
                return new Vector3(p[0], p[1], -p[2]);
            else
                return new Vector3(p[0], p[1], p[2]);
        }

        /// <summary>
        /// Gets Quaternion Rotation from a PhaseSpace Pose.
        /// Optionally performs Unity left-handed negation and reordering.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Quaternion PoseRotation(float[] p, bool raw = false)
        {
            if (!raw)
                return new Quaternion(-p[4], -p[5], p[6], p[3]);
            else
                return new Quaternion(p[3], p[4], p[5], p[6]);
        }

        /// <summary>
        /// Parses DeviceInfo status for button press states.
        /// Sort-of deprecated by DriverStatus Event
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool ButtonPressed(DeviceInfo info)
        {
            bool button, toggle;
            return ButtonPressed(info, out button, out toggle);
        }

        /// <summary>
        /// Parses DeviceInfo status for button press states.
        /// Sort-of deprecated by DriverStatus Event
        /// </summary>
        /// <param name="info"></param>
        /// <param name="button"></param>
        /// <param name="toggle"></param>
        /// <returns></returns>
        public static bool ButtonPressed(DeviceInfo info, out bool button, out bool toggle)
        {
            button = false;
            toggle = false;
            if (info.type == "driver" || info.type == "microdriver")
            {
                if (info.status.Contains("signal"))
                {
                    string[] chunks = info.status.Split(' ', '=');
                    for (int i = 0; i < chunks.Length; i++)
                    {
                        if (chunks[i] == "signal")
                        {
                            byte signal = byte.Parse(chunks[i + 1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                            byte buttons = (byte)(((byte)(signal & 0xF0)) >> 4);

                            button = (buttons & 0x01) > 0;
                            toggle = (buttons & 0x02) > 0;
                            //button is currently pressed
                            return (buttons & 0x01) > 0;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Parses DeviceInfo status and outputs all the useful bits.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="buttons"></param>
        /// <param name="batteryFlags"></param>
        /// <param name="signal"></param>
        /// <param name="capacity"></param>
        /// <param name="version"></param>
        /// <param name="encoded"></param>
        public static void GetDriverStatus(DeviceInfo info, out bool[] buttons, out bool[] batteryFlags, out float signal, out float capacity, out string version, out System.DateTime encoded)
        {
            buttons = new bool[4];
            batteryFlags = new bool[4];
            capacity = 0;
            signal = 0;
            version = "";
            encoded = System.DateTime.MinValue;

            if (info.type == "driver" || info.type == "microdriver")
            {
                //if (info.status.Contains("signal"))
                //{
                string[] chunks = info.status.Split(' ', '=');
                for (int i = 0; i < chunks.Length; i++)
                {
                    if (chunks[i] == "signal")
                    {
                        byte signalByte = byte.Parse(chunks[i + 1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                        byte buttonNibble = (byte)(((byte)(signalByte & 0xF0)) >> 4);
                        byte signalNibble = (byte)(((byte)(signalByte & 0x0F)));

                        signal = ((float)signalNibble / (float)0xF);

                        buttons[0] = (buttonNibble & 0x01) > 0;
                        buttons[1] = (buttonNibble & 0x02) > 0;
                        buttons[2] = (buttonNibble & 0x04) > 0;
                        buttons[3] = (buttonNibble & 0x08) > 0;
                        i++;
                    }
                    else if (chunks[i] == "battery")
                    {
                        byte batteryByte = byte.Parse(chunks[i + 1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                        byte stateNibble = (byte)(((byte)(batteryByte & 0xF0)) >> 4);
                        byte capacityNibble = (byte)(((byte)(batteryByte & 0x0F)));

                        capacity = ((float)capacityNibble / (float)0xF);

                        batteryFlags[0] = (stateNibble & 0x01) > 0;
                        batteryFlags[1] = (stateNibble & 0x02) > 0;
                        batteryFlags[2] = (stateNibble & 0x04) > 0;
                        batteryFlags[3] = (stateNibble & 0x08) > 0;
                        i++;
                    }
                    else if (chunks[i] == "version")
                    {
                        version = chunks[i + 1];
                        i++;
                    }
                    else if (chunks[i] == "encoded")
                    {
                        try
                        {
                            System.DateTime.TryParse(chunks[i + 1], out encoded);
                        }
                        catch
                        {
                            Debug.Log(chunks[i + 1]);
                        }

                        i++;
                    }
                }
                //}
            }
        }
    }

    #region Enumerations
    /// <summary>
    /// Event socket type
    /// </summary>
    public enum StreamingMode
    {
        Disabled = 0,
        TCP = 1,
        UDP = 2,
        UDPBroadcast = 3
    };

    /// <summary>
    /// Client Permission
    /// </summary>
    public enum SlaveMode
    {
        /// <summary>
        /// First to connect, can start and stop cameras.
        /// </summary>
        Master = 0,
        /// <summary>
        /// Typical Listener
        /// Can still use Packing
        /// </summary>
        Slave = 1,
        /// <summary>
        /// Elevated permissions to modify system-wide Trackers
        /// Can set DeviceOptions
        /// </summary>
        SlaveWithTrackers = 2,
        /// <summary>
        /// Same as SlaveWithTrackers, but now with Filters!
        /// </summary>
        SlaveWithTrackersAndFilters = 3
    }

    /// <summary>
    /// Action the PhaseSpace server takes when clients disconnect.
    /// </summary>
    public enum KeepAliveMode
    {
        /// <summary>
        /// Standard behaviour, when Master disconnects OWL stops.
        /// </summary>
        None = 0,
        /// <summary>
        /// Master must call Done with KeepAlive set to None in order to stop OWL.
        /// </summary>
        Implicit = 1,
        /// <summary>
        /// OWL stops when there are no clients connected.
        /// </summary>
        NoClients = 2
    }

    /// <summary>
    /// Arbitary tracking condition usability levels
    /// </summary>
    public enum TrackingCondition
    {
        Undefined, Invalid, Poor, Normal, Good, Great
    }

    /// <summary>
    /// PhaseSpace OWL Event Frame types
    /// </summary>
    public enum FrameEventType
    {
        Raw = 0x01,
        Peaks = 0x02,
        Planes = 0x04,
        Markers = 0x08,
        MarkerVelocities = 0x10,
        Rigids = 0x20,
        RigidVelocities = 0x40,
        TTLFrameCount = 0x80,
        Hub = 0x100,
        RX = 0x200,
        Inputs = 0x400,
        Readout = 0x800,
        Info = 0x1000,
        Status = 0x2000,
        DriverStatus = 0x4000
    }
    #endregion

    #region Output Types
    /// <summary>
    /// PhaseSpace Input container
    /// </summary>
    public class Input
    {
        public ulong hwid = 0;
        public ulong flags = 0;
        public long time = 0;
        public byte[] data = null;

        public Input()
        {

        }

        public Input(ulong hwid, ulong flags, long time, byte[] data)
        {
            this.hwid = hwid;
            this.flags = flags;
            this.time = time;
            this.data = data;
        }

        public Input(OWL.Input i)
        {
            hwid = i.hw_id;
            flags = i.flags;
            time = i.time;
            data = i.data;
        }

        public virtual void Update(OWL.Input i)
        {
            flags = i.flags;
            time = i.time;
            data = i.data;
        }

        public virtual void Update(byte[] data, long time)
        {
            if (this.data == null || (this.data.Length != data.Length && data.Length != 0))
                this.data = new byte[data.Length];

            if (data.Length > 0)
                System.Array.Copy(data, this.data, data.Length);

            this.time = time;
        }

        public override string ToString()
        {
            return string.Format("[Input] hwid={0} dataLen={2} data={3}", "0x" + hwid.ToString("x32").TrimStart('0'), time, data.Length, OWLEditorUtilities.GetByteArrayString(data));
        }
    }

    /// <summary>
    /// Specialized XBee Input Container
    /// </summary>
    public class XBeeInput : Unity.Input
    {
        public static ulong GetHWID(ushort radioAddress)
        {
            return ((ulong)0x7862656500000000 + radioAddress);
        }

        static string btos(params byte[] bytes)
        {
            string str = "";
            foreach (var b in bytes)
            {
                str += b.ToString("x2") + ",";
            }

            return str.TrimEnd(',');
        }

        public bool[] Buttons;
        public byte Counter;
        public byte[] Payload;

        public XBeeInput(ulong hwid, ulong flags, long time, byte[] data)
        {
            Buttons = new bool[8];
            Payload = new byte[8];

            this.hwid = hwid;
            this.flags = flags;
            this.time = time;
            this.data = data;
        }

        public XBeeInput(OWL.Input i) : base(i)
        {
            Buttons = new bool[8];
            Payload = new byte[8];

            hwid = i.hw_id;
            flags = i.flags;
            time = i.time;
            data = i.data;
        }

        public ushort RadioAddress
        {
            get
            {
                return (ushort)(hwid & 0xFFFF);
            }
        }

        public override void Update(OWL.Input i)
        {
            base.Update(i);

            for (int n = 0; n < 8; n++)
                Buttons[n] = (data[0] & (1 << n)) > 0;

            Counter = data[1];
            System.Array.Copy(data, 2, Payload, 0, 8);
        }

        public override void Update(byte[] data, long time)
        {
            base.Update(data, time);
            for (int n = 0; n < 8; n++)
                Buttons[n] = (data[0] & (1 << n)) > 0;

            Counter = data[1];
            System.Array.Copy(data, 2, Payload, 0, 8);
        }

        /// <summary>
        /// Send data back to XBee via Context DeviceOptions
        /// </summary>
        /// <param name="context"></param>
        /// <param name="payload"></param>
        public void Send(Context context, params byte[] payload)
        {
            context.deviceOptions(hwid, "request=tx16 data=" + btos(payload));
        }

        /// <summary>
        /// Send string back to XBee via Context DeviceOptions
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public void Send(Context context, string message)
        {
            Send(context, System.Text.Encoding.ASCII.GetBytes(message));
        }

        public override string ToString()
        {
            string p = "";
            foreach (var b in Payload)
            {
                p += b.ToString("x2") + " ";
            }
            return string.Format("[XBeeInput] RadioAddress={0} Buttons={2} Counter={3} Payload={4}", RadioAddress.ToString("x4"), time, System.Convert.ToString(data[0], 2), Counter, p);
        }
    }

    /// <summary>
    /// Marker container
    /// </summary>
    public class Marker
    {
        public uint id;
        public uint flags;
        public long time;
        public float cond;
        public Vector3 position;
        public Vector3 velocity;

        public Marker(uint id)
        {
            this.id = id;
        }

        public Marker(uint id, uint flags, long time, float cond, Vector3 pos)
        {
            this.id = id;
            this.flags = flags;
            this.time = time;
            this.cond = cond;
            this.position = pos;
        }

        public void Update(OWL.Marker m)
        {
            this.flags = m.flags;
            this.time = m.time;
            this.cond = m.cond;
            this.position.Set(m.x, m.y, m.z);
        }

        public void Update(OWL.Marker m, Matrix4x4 matrix)
        {
            Update(m);
            this.position = matrix.MultiplyPoint3x4(this.position);
        }

        public TrackingCondition Condition
        {
            get
            {
                if (cond == 0)
                    return TrackingCondition.Undefined;

                if (cond < 0)
                    return TrackingCondition.Invalid;

                if (cond < 5)
                    return TrackingCondition.Great;

                if (cond < 10)
                    return TrackingCondition.Good;

                if (cond < 20)
                    return TrackingCondition.Normal;

                return TrackingCondition.Poor;
            }
        }

        public uint Slot
        {
            get
            {
                return flags & 0xF;
            }
        }

        public bool Predicted
        {
            get
            {
                return (flags & 0x10) > 0;
            }
        }

        public bool Rejected3D
        {
            get
            {
                return (flags & 0x100) > 0;
            }
        }
    }

    /// <summary>
    /// Rigid container
    /// </summary>
    public class Rigid
    {
        public uint id;
        public uint flags;
        public long time;
        public float cond;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        public void SetRotation(float w, float x, float y, float z)
        {
            rotation.Set(x, y, z, w);
        }

        public Rigid(uint id)
        {
            this.id = id;
        }

        public Rigid(uint id, uint flags, long time, float cond, Vector3 pos, Quaternion rot)
        {
            this.id = id;
            this.flags = flags;
            this.time = time;
            this.cond = cond;
            this.position = pos;
            this.rotation = rot;
        }

        public void Update(OWL.Rigid r)
        {
            float[] p = r.pose;
            flags = r.flags;
            time = r.time;
            cond = r.cond;
            position.Set(p[0], p[1], p[2]);
            rotation.Set(p[4], p[5], p[6], p[3]);
        }

        public TrackingCondition Condition
        {
            get
            {
                if (cond == 0)
                    return TrackingCondition.Undefined;

                if (cond < 0)
                    return TrackingCondition.Invalid;

                if (cond > 6)
                    return TrackingCondition.Great;

                if (cond > 4)
                    return TrackingCondition.Good;

                if (cond > 2)
                    return TrackingCondition.Normal;

                return TrackingCondition.Poor;
            }
        }

        public bool KalmanActive
        {
            get
            {
                return (flags & 0x04) > 0;
            }
        }

        public bool OffsetsActive
        {
            get
            {
                return (flags & 0x08) > 0;
            }
        }

        public bool PredictionActive
        {
            get
            {
                return (flags & 0x10) > 0;
            }
        }
    }

    /// <summary>
    /// Camera Position, Rotation, general information
    /// </summary>
    public class Camera
    {
        public string alias;
        public ulong hwid;
        public uint id;
        public uint flags;
        public float cond;
        public Vector3 position;
        public Quaternion rotation;
        public uint port;
        public bool rf;
        public bool missing;

        public Camera(uint id, uint flags, float cond, Vector3 pos, Quaternion rot, uint port = 0, bool rf = false)
        {
            this.id = id;
            this.flags = flags;
            this.cond = cond;
            this.position = pos;
            this.rotation = rot;
            this.rf = rf;
        }

        public Camera(DeviceInfo info)
        {
            Update(info);
        }

        public Camera(OWL.Camera c)
        {
            id = c.id;
            Update(c);
        }

        public void Update(OWL.Camera c)
        {
            float[] p = c.pose;
            flags = c.flags;
            cond = c.cond;
            position.Set(p[0], p[1], p[2]);
            rotation.Set(p[4], p[5], p[6], p[3]);
        }

        public void Update(OWL.DeviceInfo info)
        {
            if (hwid == info.hw_id)
                return;

            this.hwid = info.hw_id;
            this.missing = info.name.StartsWith("missing");
            this.id = uint.Parse(missing ? info.name.Replace("missing", "") : info.name);

            var tokens = info.status.Split(new char[] { '=', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int t = 0; t < tokens.Length; t++)
            {
                switch (tokens[t])
                {
                    case "rfid":
                        this.rf = true;
                        t++;
                        continue;
                    case "port":
                        this.port = uint.Parse(tokens[t + 1]);
                        t++;
                        continue;
                }
            }

            tokens = info.options.Split(new char[] { '=', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int t = 0; t < tokens.Length; t++)
            {
                switch (tokens[t])
                {
                    case "alias":
                        this.alias = tokens[t + 1];
                        t++;
                        continue;
                }
            }

            if (alias == null || alias == "")
                alias = id.ToString();
        }

        public TrackingCondition Condition
        {
            get
            {
                if (cond == 0)
                    return TrackingCondition.Undefined;

                if (cond > 0)
                    return TrackingCondition.Great;

                return TrackingCondition.Invalid;
            }
        }

        public override string ToString()
        {
            return string.Format("[Camera] alias={0} id={1} port={2} rf={3} missing={4} hwid={5}", alias, id, port, rf, missing, hwid);
        }
    }

    /// <summary>
    /// Individual peak data from a single detector of a camera
    /// </summary>
    public class Peak
    {
        public uint id;
        public uint flags;
        public long time;
        public uint camera;
        public uint detector;
        public uint width;
        public float pos;
        public float amp;

        public Peak(uint id, uint flags, long time, uint camera, uint detector, uint width, float pos, float amp)
        {
            this.id = id;
            this.flags = flags;
            this.time = time;
            this.camera = camera;
            this.detector = detector;
            this.width = width;
            this.pos = pos;
            this.amp = amp;
        }
    }
    #endregion

    #region Data Containers
    /// <summary>
    /// Server information reported from OWL Scan packets
    /// </summary>
    public class ServerInfo
    {
        public string address;
        public string info;

        public ServerInfo()
        {
            address = "";
            info = "";
        }

        public ServerInfo(string address, string info)
        {
            this.address = address;
            this.info = info;
        }
    }

    //TODO:  Implement periodic and multiphasic pack decoding
    /// <summary>
    /// Decoded Packing information
    /// </summary>
    public class PackCache
    {
        public Type typeId;
        public ushort id;
        public ushort size;
        public string name;
        public ulong[] ids;

        public PackCache(PackInfo info)
        {
            typeId = info.type_id;
            id = info.id;

            var chunks = info.options.Split(',', '=', ' ');
            for (int i = 0; i < chunks.Length; i++)
            {
                switch (chunks[i])
                {
                    case "size":
                        size = ushort.Parse(chunks[i + 1]);
                        i++;
                        break;
                }
            }
            name = info.name;
            ids = info.ids;
        }
    }

    public class Driver
    {
        public enum DeviceType { Unknown, Microdriver, Driver }

        public DeviceInfo Info;
        public DeviceType Type;
        public bool[] Buttons = new bool[2];
        public bool[] Toggles = new bool[2];
        public float Signal;
        public float Battery;
        public bool Charging;
        public bool Powered;
        public string Status
        {
            get
            {
                return Info.status;
            }
        }

        /// <summary>
        /// Triggered whenever a Driver updates its button, signal, battery, or other states.
        /// Not thread safe.
        /// </summary>
        public event System.Action<Driver> OnUpdated;

        public Driver(ulong hwid)
        {
            DeviceInfo info = new DeviceInfo(hwid, -1, "driver", "");
            Info = info;
            Type = DeviceType.Unknown;
        }

        public Driver(DeviceInfo info)
        {
            Info = info;
            switch (info.type)
            {
                case "driver":
                    Type = DeviceType.Driver;
                    break;
                case "microdriver":
                    Type = DeviceType.Microdriver;
                    break;
            }
        }

        public void Update(byte[] data)
        {
            byte signalByte = data[0];
            byte buttonNibble = (byte)(((byte)(signalByte & 0xF0)) >> 4);
            byte signalNibble = (byte)(((byte)(signalByte & 0x0F)));

            Signal = ((float)signalNibble / (float)0xF);

            Buttons[0] = (buttonNibble & 0x01) > 0;
            Toggles[0] = (buttonNibble & 0x02) > 0;
            Buttons[1] = (buttonNibble & 0x04) > 0;
            Toggles[1] = (buttonNibble & 0x08) > 0;

            byte stateByte = data[1];
            byte stateNibble = (byte)(((byte)(stateByte & 0xF0)) >> 4);
            byte capacityNibble = (byte)(((byte)(stateByte & 0x0F)));

            Battery = ((float)capacityNibble / (float)0xF);

            Charging = (stateNibble & 0x4) > 0;
            Powered = (stateNibble & 0x8) > 0;

            if (OnUpdated != null)
                OnUpdated(this);
        }

        public void Update(ulong flags)
        {
            Update(new byte[] { (byte)(flags & 0xFF), (byte)((flags & 0xFF00) >> 8) });
        }

        public override string ToString()
        {
            return string.Format("[Driver] HWID={0}, Signal={1}, Battery={2}, Charging={3}, Powered={4}, Buttons={5}, Toggles={6}", Info.hw_id.ToString("x2"), Signal.ToString("f2"), Battery.ToString("f2"), Charging.ToString(), Powered.ToString(), (Buttons[0] ? "1" : "0") + (Buttons[1] ? "1" : "0"), (Toggles[0] ? "1" : "0") + (Toggles[1] ? "1" : "0"));
        }
    }
    #endregion

}

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

//#define OWL_THREADING

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Linq;
using System.IO;
using UnityEngine;
using PhaseSpace.OWL;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PhaseSpace.Unity
{
    public class OWLClient : MonoBehaviour
    {


        public enum ConnectionState { Unknown, Closed, Opening, Open, Initializing, Initialized, Streaming }

        static OWLClient singleton;

        [Tooltip("Persist through Scene Changes. Acts like Singleton.")]
        public bool Persistent;
        [Tooltip("Automatically Scan for first available server")]
        public bool AutoScan;
        [Tooltip("Automatically Open a connection to Server Address")]
        public bool AutoOpen;
        [Tooltip("Automatically Initialize using current settings")]
        public bool AutoInitialize;
        [Tooltip("IP Address of OWL Server")]
        public string ServerAddress = "127.0.0.1";
        public float OpenTimeout = 5;
        [Tooltip("Socket Type for streaming frame based data")]
        public StreamingMode StreamingMode = StreamingMode.TCP;
        [Tooltip("Frames per second sent from OWL Server")]
        public uint Frequency = 90;
        [Tooltip("Permission level")]
        public SlaveMode SlaveMode = SlaveMode.Master;
        [Tooltip("Profile to initialize if connecting as Master")]
        public string Profile = "default";
        [Tooltip("Special mode for Calibration only")]
        public bool CalibrationMode = false;
        [Tooltip("Overrides PhaseSpace profiles.json")]
        public OWLProfile EmbeddedProfile = null;
        [Tooltip("Overrides PhaseSpace devices.json")]
        public OWLDevices EmbeddedDevices = null;
        [Tooltip("Mask for received OWL Events")]
        public int FrameEventMask = (int)(FrameEventType.Raw | FrameEventType.Markers | FrameEventType.Rigids | FrameEventType.Info | FrameEventType.Status | FrameEventType.DriverStatus);
        [Tooltip("Implict: OWL must be shut down by Master   NoClients: OWL is shut down when no clients remain connected")]
        public KeepAliveMode KeepAlive = KeepAliveMode.None;
        [Tooltip("Overrides system-wide LED Power")]
        public bool OverrideLEDPower = false;
        [Range(0f, 1f)]
        public float LEDPower = 0.3f;

        //Ignore parameter
        public float Scale = 0.001f;

        [Tooltip("Create Rigid Trackers from OWLRigidData in this array on Initialize")]
        public OWLRigidData[] InitialRigidbodies;

        [Tooltip("Unity Editor SceneView Marker Gizmos")]
        public bool MarkerGizmos = true;
        [Tooltip("Unity Editor SceneView Rigid Gizmos")]
        public bool RigidGizmos = true;
        [Tooltip("Unity Editor SceneView Camera Gizmos")]
        public bool CameraGizmos = true;

        //packing
        public List<ulong> PackedMarkers;
        public List<ulong> PackedMarkerVelocities;
        public List<ulong> PackedRigids;
        public List<ulong> PackedRigidVelocities;
        [HWID]
        public List<ulong> PackedInputs;
        [HWID]
        public List<ulong> PackedDrivers;
        [HWID]
        public List<ushort> PackedXBees;
        [Tooltip("Enable Pack mode on Initialize")]
        public bool PackOnInitialize;

        //tables
        /// <summary>
        /// HWID indexed table of known Drivers
        /// </summary>
        public Dictionary<ulong, Driver> Drivers = new Dictionary<ulong, Driver>();

        /// <summary>
        /// Camera ID indexed table of Cameras
        /// </summary>
        Dictionary<uint, Camera> cameraIDLookup = new Dictionary<uint, Camera>();

        /// <summary>
        /// Camera ID indexed table of Missing Cameras
        /// </summary>
        Dictionary<uint, Camera> missingCameraIDLookup = new Dictionary<uint, Camera>();

        /// <summary>
        /// HWID indexed table of Cameras
        /// </summary>
        Dictionary<ulong, Camera> cameraHWIDLookup = new Dictionary<ulong, Camera>();

        //tracking data
        /// <summary>
        /// List of all possible Markers' position, velocity, condition, and state.  Data may become stale.
        /// </summary>
        public readonly List<Marker> Markers = new List<Marker>();

        /// <summary>
        /// List of all possible Rigids position, velocity, rotation, angular velocity, condition, and state.  Data may be come stale.
        /// </summary>
        public readonly List<Rigid> Rigids = new List<Rigid>();

        /// <summary>
        /// List of all known Cameras.
        /// </summary>
        public readonly List<Camera> Cameras = new List<Camera>();

        /// <summary>
        /// List of all missing Cameras.
        /// </summary>
        public readonly List<Camera> MissingCameras = new List<Camera>();

        /// <summary>
        /// HWID indexed table of Inputs
        /// Drivers, XBees are included in this table
        /// </summary>
        public readonly Dictionary<ulong, Input> Inputs = new Dictionary<ulong, Input>();

        /// <summary>
        /// XBee Radio ID indexed table of specialized XBeeInputs
        /// </summary>
        public readonly Dictionary<ushort, XBeeInput> XBeeInputs = new Dictionary<ushort, XBeeInput>();

        //tracking events -- NOT BUFFERED, NOT THREADSAFE
        /// <summary>
        /// Event fired when receiving Peaks.  Typically only used during Calibration
        /// Not Thread Safe
        /// </summary>
        public event System.Action<PhaseSpace.OWL.Peak[]> OnReceivedPeaks;

        /// <summary>
        /// Event fired when receiving generic byte data
        /// Not Thread Safe
        /// </summary>
        public event System.Action<PhaseSpace.OWL.Event> OnReceivedBytes;

        /// <summary>
        /// Event fired when receiving DeviceInfo
        /// Not Thread Safe
        /// </summary>
        public event System.Action<PhaseSpace.OWL.Event> OnReceivedDeviceInfo;

        #region Properties
        /// <summary>
        /// The current Connection State of OWLClient, typically also the state of OWL Context.
        /// </summary>
        public ConnectionState State
        {
            get
            {
                return state;
            }
        }

        /// <summary>
        /// List of all available profile names.  Available after Open.
        /// </summary>
        public string[] Profiles
        {
            get
            {
                if (context == null)
                    return new string[0];

                return context.property<string[]>("profiles");
            }
        }

        /// <summary>
        /// Tracking is ready!
        /// </summary>
        public bool Ready
        {
            get
            {
                return state >= ConnectionState.Initialized;
            }
        }

        /// <summary>
        /// Direct getter for Context instance.
        /// Usually not thread safe.
        /// </summary>
        public Context Context
        {
            get
            {
                return context;
            }
        }
        #endregion

        protected Context context;
        protected ConnectionState state;
        protected List<PackCache> packCache = new List<PackCache>();

        #region Startup
        /// <summary>
        /// Start everything up.
        /// </summary>
        /// <returns></returns>
        IEnumerator Start()
        {
            if (Persistent)
            {
                if (singleton == null || singleton != this)
                {
                    if (singleton != null)
                    {
                        Destroy(gameObject);
                        yield break;
                    }
                    else
                    {
                        singleton = this;
                        DontDestroyOnLoad(gameObject);
                    }
                }
            }

            state = ConnectionState.Closed;

#if OWL_THREADING
            StartPollingThread();
#endif
            if (AutoScan)
            {
                OWLScan.Active = true;
                while (OWLScan.Servers.Length == 0)
                    yield return null;
                OWLScan.Active = false;

                ServerAddress = OWLScan.Servers[0].address;
            }

            if (AutoOpen)
            {

                if (AutoOpen)
                {
                    Open();

                    while (state < ConnectionState.Open)
                        yield return null;

                    if (AutoInitialize)
                        Initialize();
                }
            }
        }

        #endregion


        #region Connecting
        /// <summary>
        /// Opens a connection to a PhaseSpace server.
        /// </summary>
        public Coroutine Open()
        {
            if (state != ConnectionState.Closed)
                return null;

            return StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            state = ConnectionState.Opening;
            context = new Context();
            //context.debug = true;

            try
            {
                float startTime = Time.realtimeSinceStartup;

                if (EmbeddedProfile != null)
                    Profile = EmbeddedProfile.name;



                string serverIp = ServerAddress;

                //cannot resolve, use OWLScan
                System.Net.IPAddress addr;
                if (!System.Net.IPAddress.TryParse(ServerAddress, out addr))
                {
                    OWLScan.Active = true;
                    bool serverFound = false;
                    while (!serverFound)
                    {
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

                            if (hostname == ServerAddress)
                            {
                                serverIp = s.address;
                                serverFound = true;
                                break;
                            }

                        }
                        yield return null;
                    }
                    OWLScan.Active = false;
                }

                Debug.Log("[OWL] Opening\n" + ServerAddress);

                while (!context.isOpen() && state == ConnectionState.Opening)
                {
                    //TODO:  Detect IP address from Hostname to deal with bad networks

                    int ret = context.open(serverIp, "timeout=0");

                    switch (ret)
                    {
                        case -1:
                            //error
                            Debug.LogError("[OWL] " + context.lastError());
                            state = ConnectionState.Open;
                            break;
                        case 0:
                            //check for async timeout since owl.cs handles this weird
                            if (Time.realtimeSinceStartup - startTime > OpenTimeout)
                            {
                                context.close();
                                state = ConnectionState.Closed;
                            }
                            break;
                        case 1:
                            //success
                            Debug.Log("[OWL] Open\nCore " + context.property("coreversion"));
                            state = ConnectionState.Open;

                            //disable system devices
                            if (SlaveMode == SlaveMode.Master && EmbeddedDevices != null)
                            {
                                context.options("system.enableDevicesConfig=0");
                                if (EmbeddedDevices != null)
                                    UpdateDevices();
                            }

                            //push embedded profile
                            if (SlaveMode == SlaveMode.Master && EmbeddedProfile != null)
                            {
                                context.options("profile.json=" + EmbeddedProfile.GetJSON());
                            }

                            gameObject.SendMessage("OnOWLOpened", SendMessageOptions.DontRequireReceiver);

                            break;
                    }

                    yield return null;
                }
            }
            finally
            {

            }
        }

        /// <summary>
        /// Initializes a session after Open succeeds.
        /// </summary>
        public Coroutine Initialize()
        {
            if (state != ConnectionState.Open)
                return null;

            return StartCoroutine(InitializeRoutine());
        }

        IEnumerator InitializeRoutine()
        {
            Debug.Log("[OWL] Initializing");
            //init tracking data structs
            Markers.Clear();
            //allocate after initialize

            Rigids.Clear();
            //pre allocate 64 rigids
            for (int i = 0; i < 64; i++)
                Rigids.Add(new Rigid((uint)i));

            Cameras.Clear();
            MissingCameras.Clear();
            //pre allocate 80 cameras because occassional odd startup order
            //for (int i = 0; i < 80; i++)
            //{
            //    Cameras.Add(null);
            //    MissingCameras.Add(null);
            //}


            cameraIDLookup.Clear();
            cameraHWIDLookup.Clear();
            Drivers.Clear();

            state = ConnectionState.Initializing;
            int ret = 0;

            string initStr = "";
            AppendToken(ref initStr, "timeout", 0);

            AppendToken(ref initStr, "slave", (int)SlaveMode);
            switch (SlaveMode)
            {
                case SlaveMode.Master:
                    //add profile token to master only
                    if (CalibrationMode)
                        AppendToken(ref initStr, "profile", "calibration");
                    else
                        AppendToken(ref initStr, "profile", EmbeddedProfile == null ? Profile : EmbeddedProfile.name);

                    if (KeepAlive > 0)
                        AppendToken(ref initStr, "keepalive", (int)KeepAlive);

                    if (OverrideLEDPower)
                        AppendToken(ref initStr, "system.LEDPower", LEDPower);

                    break;
                default:
                    break;
            }

            //enable streaming only after allocating markers
            //AppendToken(ref initStr, "streaming", (int)StreamingMode);

            AppendToken(ref initStr, "frequency", Frequency);

            //frame event mask options
            GetFrameEventMaskOptions(ref initStr);

            //TODO:  implement scale, for now default to 1:1 and use Unity scene graph
            AppendToken(ref initStr, "scale", Scale);

            //Unity-ness
            AppendToken(ref initStr, "lefthanded", 1);

            if (PackOnInitialize)
                AppendToken(ref initStr, "pack", 1);

            //Debug.Log(initStr);

            //initStr = AppendToken(initStr, "event.internal", 2);

            if (SlaveMode != SlaveMode.Master)
            {
                //wait until system is streaming
                while (context.property<string>("systemstatus") != "initialized")
                {
                    Debug.LogWarning("[OWL] Waiting for System to be Initialized");
                    yield return new WaitForSeconds(0.5f);
                }
            }

            while (ret == 0)
            {
                lock (this)
                {
                    ret = context.initialize(initStr);
                }

                yield return new WaitForSeconds(0.1f);
            }

            switch (ret)
            {
                case -1:
                    //fail
                    Debug.LogError("[OWL] " + context.lastError());
                    state = ConnectionState.Closed;
                    break;
                case 1:
                    //success

                    for (int i = 0; i < Cameras.Count; i++)
                    {
                        if (Cameras[i] == null)
                        {
                            Cameras.RemoveRange(i, Cameras.Count - i);
                            break;
                        }
                    }

                    for (int i = 0; i < MissingCameras.Count; i++)
                    {
                        if (MissingCameras[i] == null)
                        {
                            MissingCameras.RemoveRange(i, MissingCameras.Count - i);
                            break;
                        }
                    }

                    int markerCount = context.property<int>("markers");
                    for (int i = 0; i < (markerCount == 0 ? 512 : markerCount); i++)
                        Markers.Add(new Marker((uint)i));

                    Debug.Log("[OWL] Initialized");
                    state = ConnectionState.Initialized;
                    gameObject.SendMessage("OnOWLInitialized", SendMessageOptions.DontRequireReceiver);

                    //finish initializing packing 
                    UpdatePackInfo();

                    //enable streaming after marker count initialized
                    context.streaming((int)StreamingMode);

                    if ((int)SlaveMode != context.property<int>("slave"))
                    {
                        Debug.LogWarning("[OWL] SlaveMode set to: " + (SlaveMode)context.property<int>("slave"));
                        SlaveMode = (SlaveMode)context.property<int>("slave");
                    }

                    if (Profile != context.property<string>("profile"))
                    {
                        Debug.LogWarning("[OWL] Profile set to: " + context.property<string>("profile"));
                        Profile = context.property<string>("profile");
                    }


                    //make some rigidbodies
                    if (SlaveMode != SlaveMode.Slave)
                    {
                        foreach (var d in InitialRigidbodies)
                            CreateRigidTracker(d);
                    }

                    yield return new WaitForSeconds(1f);


                    break;
            }

        }

        /// <summary>
        /// Frame Event options helper
        /// </summary>
        /// <param name="str"></param>
        public void GetFrameEventMaskOptions(ref string str)
        {
            for (int i = 0; i < 15; i++)
            {
                int n = 1 << i;
                bool active = (FrameEventMask & n) > 0;

                //enforce at least these are on for calibration mode
                if (CalibrationMode)
                {
                    var t = (FrameEventType)n;
                    switch (t)
                    {
                        case FrameEventType.Peaks:
                        case FrameEventType.Markers:
                        case FrameEventType.Rigids:
                        case FrameEventType.Info:
                        case FrameEventType.Status:
                        case FrameEventType.DriverStatus:
                            active = true;
                            break;
                    }
                }

                if (((FrameEventType)n) == FrameEventType.Status)
                {
                    //handle exception
                    AppendToken(ref str, "event.info.status", active ? 1 : 0);
                }
                else
                {
                    AppendToken(ref str, "event." + ((FrameEventType)n).ToString().ToLower(), active ? "1" : "0");
                }
            }
        }
        #endregion

        #region Disconnecting
        /// <summary>
        /// Ends a session.
        /// </summary>
        public void Done()
        {
            Drivers.Clear();

            if (state >= ConnectionState.Initialized)
            {
                if (SlaveMode == SlaveMode.Master)
                    context.done("keepalive=" + (int)KeepAlive);
                else
                    context.done();

                state = ConnectionState.Open;
            }
        }

        /// <summary>
        /// Disconnects from OWL server.
        /// </summary>
        public void Close()
        {
            if (context != null && context.isOpen())
            {
                context.close();
                state = ConnectionState.Closed;
            }
        }
        #endregion

        #region Internal
        string AppendToken(ref string str, string token, object value)
        {
	    return AppendToken(ref str, token, value.ToString());
        }
        string AppendToken(ref string str, string token, float value)
        {
	    return AppendToken(ref str, token, value.ToString(new CultureInfo("en-US")));
        }
        string AppendToken(ref string str, string token, string value)
        {
            str += token + "=" + value + " ";
            return str;
        }
        #endregion



        #region Cleanup
        void OnDestroy()
        {
            if (OWLScan.Active)
                OWLScan.Active = false;

            Done();
            Close();
#if OWL_THREADING
            pollingThread.Abort();
#endif
        }
        #endregion

        #region Streaming
        /// <summary>
        /// Sets OWL Option
        /// </summary>
        /// <param name="opts"></param>
        public void SetOption(string opts, bool quiet = false)
        {
            if (!quiet)
                Debug.Log("[OWL] SetOption: " + opts);
            context.options(opts);
        }

        /// <summary>
        /// Gets OWL Option
        /// </summary>
        /// <param name="opt"></param>
        /// <returns></returns>
        public string GetOption(string opt)
        {
            return context.option(opt);
        }

        /// <summary>
        /// Gets typed OWL Property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public T GetProperty<T>(string property)
        {
            return context.property<T>(property);
        }

        /// <summary>
        /// Disables Packing
        /// </summary>
        public void DisablePacking()
        {
            context.options("pack=0");
        }

        /// <summary>
        /// Enables Packing
        /// </summary>
        public void EnablePacking()
        {
            context.options("pack=1");
        }

        /// <summary>
        /// Updates PackInfo on OWL server for this client
        /// </summary>
        public void UpdatePackInfo()
        {
            if (state < ConnectionState.Initialized)
            {
                Debug.LogWarning("[OWL] Cannot Update PackInfo before intializing!");
                return;
            }


            PackInfo packMarkers = new PackInfo(Type.MARKER, "markers", "unity=1", PackedMarkers.ToArray());
            PackInfo packMarkerVelocities = new PackInfo(Type.FLOAT, "markervelocities", "unity=1", PackedMarkerVelocities.ToArray());
            PackInfo packRigids = new PackInfo(Type.RIGID, "rigids", "unity=1", PackedRigids.ToArray());
            PackInfo packRigidVelocities = new PackInfo(Type.FLOAT, "rigidvelocities", "unity=1", PackedRigidVelocities.ToArray());
            PackInfo packInputs = new PackInfo(Type.INPUT, "inputs", "", PackedInputs.ToArray());
            PackInfo packDrivers = new PackInfo(Type.INPUT, "driverstatus", "size=2 flags=1", PackedDrivers.ToArray());
            List<ulong> xbeeHWIDs = new List<ulong>();
            foreach (var x in PackedXBees)
                xbeeHWIDs.Add(XBeeInput.GetHWID(x));

            PackInfo packXBees = new PackInfo(Type.INPUT, "inputs", "size=10", xbeeHWIDs.ToArray());

            List<PackInfo> packs = new List<PackInfo>();
            if (packMarkers.ids.Length > 0)
                packs.Add(packMarkers);
            if (packMarkerVelocities.ids.Length > 0)
                packs.Add(packMarkerVelocities);
            if (packRigids.ids.Length > 0)
                packs.Add(packRigids);
            if (packRigidVelocities.ids.Length > 0)
                packs.Add(packRigidVelocities);
            if (packInputs.ids.Length > 0)
                packs.Add(packInputs);
            if (packXBees.ids.Length > 0)
                packs.Add(packXBees);
            if (packDrivers.ids.Length > 0)
                packs.Add(packDrivers);

            context.pack(packs.ToArray());
        }

        /// <summary>
        /// Polling
        /// </summary>
        void Update()
        {
            //editor work around for serializing devices database on demand when in threading mode
#if OWL_THREADING && UNITY_EDITOR
            if (EmbeddedDevices != null)
                EmbeddedDevices.ApplyChanges();
#endif

#if !OWL_THREADING
            Poll();
#endif
        }
#if OWL_THREADING
        Thread pollingThread;
        void StartPollingThread()
        {
            ThreadStart start = new ThreadStart(PollingThread);
            pollingThread = new Thread(start);
            pollingThread.Start();
        }

        void PollingThread()
        {
            while (true)
            {
                Poll();
                Thread.Sleep(1);
            }
        }
#endif

        /// <summary>
        /// Processes data received when Packing
        /// </summary>
        /// <param name="e"></param>
        void ReadPackedData(PhaseSpace.OWL.Event e)
        {
            MemoryStream mem = new MemoryStream(e.data as byte[]);
            BinaryReader br = new BinaryReader(mem);

            try
            {
                foreach (var p in packCache)
                {
                    if (e.id != (p.id >> 8))
                        continue;

                    switch (p.typeId)
                    {
                        case Type.FLOAT:
                            switch (p.name)
                            {
                                case "markervelocities":
                                    foreach (var id in p.ids)
                                    {
                                        var m = Markers[(int)id];
                                        m.velocity.Set(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                    }
                                    break;
                                case "rigidvelocities":
                                    foreach (var id in p.ids)
                                    {
                                        var r = Rigids[(int)id];
                                        r.velocity.Set(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                        r.angularVelocity.Set(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                    }
                                    break;
                            }
                            break;
                        case Type.MARKER:
                            foreach (var id in p.ids)
                            {
                                var m = Markers[(int)id];
                                m.position.Set(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                m.cond = br.ReadInt16();
                                m.flags = br.ReadUInt16();
                            }
                            break;
                        case Type.RIGID:
                            foreach (var id in p.ids)
                            {
                                var r = Rigids[(int)id];
                                r.position.Set(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                r.rotation.Set(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                r.cond = br.ReadInt16();
                                r.flags = br.ReadUInt16();
                            }
                            break;
                        case Type.INPUT:
                            switch (p.name)
                            {
                                case "inputs":
                                    foreach (var id in p.ids)
                                    {
                                        if (!Inputs.ContainsKey(id))
                                        {
                                            byte[] hwBytes = System.BitConverter.GetBytes(id);
                                            if (hwBytes[7] == 0x78 && hwBytes[6] == 0x62 && hwBytes[5] == 0x65 && hwBytes[4] == 0x65)
                                            {
                                                var xbee = new XBeeInput(id, 0, e.time, null);
                                                Inputs.Add(id, xbee);
                                                XBeeInputs.Add(xbee.RadioAddress, xbee);
                                            }
                                            else
                                            {
                                                Inputs.Add(id, new Input(id, 0, e.time, null));
                                            }

                                            Debug.Log("[OWL] Adding input: 0x" + id.ToString("x32").TrimStart('0'));
                                        }

                                        var i = Inputs[id];
                                        ushort size = p.size == 0 ? br.ReadUInt16() : p.size;
                                        i.Update(br.ReadBytes(size), e.time);
                                    }
                                    break;
                                case "driverstatus":
                                    foreach (var id in p.ids)
                                    {
                                        //TODO: find a way to add drivers through packing
                                        if (!Drivers.ContainsKey(id))
                                        {
                                            //Drivers[id] = new Driver(id);
                                            //Debug.Log("[OWL] Adding Packed Driver: 0x" + id.ToString("x2"));
                                        }


                                        var d = Drivers.ContainsKey(id) ? Drivers[id] : null;
                                        ushort size = p.size == 0 ? br.ReadUInt16() : p.size;
                                        if (size == 2)
                                        {
                                            if (d != null)
                                                d.Update(br.ReadUInt16());
                                            else
                                                br.ReadUInt16();
                                        }
                                        else
                                        {
                                            Debug.LogWarning("[OWL] Odd size for Packed DriverStatus: " + size);
                                            br.ReadBytes(size);
                                        }
                                    }
                                    break;
                            }

                            break;

                    }
                }
            }
            catch
            {
                //read past end of stream error
            }
        }

        /// <summary>
        /// Poll and empty event queue
        /// </summary>
        void Poll()
        {
            if (context != null && State >= ConnectionState.Open)
            {
                for (int i = 0; i < 256; i++)
                {
#if OWL_THREADING
                    lock (this)
#endif
                    {

                        var e = context.nextEvent();
                        if (e == null)
                            break;

                        switch (e.type_id)
                        {
                            default:

                                break;
                            case Type.ERROR:
                                if (e.name == "warning")
                                {
                                    Debug.LogWarning("[OWL] " + System.Text.Encoding.ASCII.GetString((byte[])e.data));
                                }
                                else
                                {
                                    Debug.LogError("[OWL] " + System.Text.Encoding.ASCII.GetString((byte[])e.data) + "\r\n" + context.lastError());
                                }

                                break;
                            case Type.PACKINFO:
                                //received pack info
                                PackInfo[] infos = (PackInfo[])e.data;
                                packCache.Clear();
                                foreach (var pi in infos)
                                {
                                    packCache.Add(new PackCache(pi));
                                    if (pi.type_id == Type.INPUT)
                                    {
                                        foreach (var id in pi.ids)
                                        {
                                            if (!Inputs.ContainsKey(id))
                                            {
                                                byte[] hwBytes = System.BitConverter.GetBytes(id);
                                                if (hwBytes[7] == 0x78 && hwBytes[6] == 0x62 && hwBytes[5] == 0x65 && hwBytes[4] == 0x65)
                                                {
                                                    var xbee = new XBeeInput(id, 0, 0, new byte[10]);
                                                    Inputs.Add(id, xbee);
                                                    XBeeInputs.Add(xbee.RadioAddress, xbee);
                                                }
                                            }
                                        }
                                    }
                                }

                                break;

                            case Type.BYTE:
                                if (OnReceivedBytes != null)
                                    OnReceivedBytes(e);

                                if (e.name == "raw")
                                {
                                    //probably packed data
                                    ReadPackedData(e);
                                }

                                break;
                            case Type.FRAME:
                                if (e["markers"] != null)
                                {
                                    var mArr = (PhaseSpace.OWL.Marker[])e["markers"];
                                    for (int m = 0; m < mArr.Length; m++)
                                    {
                                        Markers[(int)mArr[m].id].Update(mArr[m]);
                                    }
                                }

                                if (e["markervelocities"] != null)
                                {
                                    var velArr = e["markervelocities"] as float[];
                                    int m = 0;
                                    for (int v = 0; v < velArr.Length; v += 3, m++)
                                    {
                                        Markers[m].velocity.Set(velArr[v], velArr[v + 1], velArr[v + 2]);
                                    }
                                }

                                if (e["rigids"] != null)
                                {
                                    var rArr = (OWL.Rigid[])e["rigids"];
                                    for (int r = 0; r < rArr.Length; r++)
                                    {
                                        Rigids[(int)rArr[r].id].Update(rArr[r]);
                                    }
                                }

                                if (e["rigidvelocities"] != null)
                                {
                                    var velArr = e["rigidvelocities"] as float[];
                                    int r = 0;
                                    for (int v = 0; v < velArr.Length; v += 6, r++)
                                    {
                                        Rigids[r].velocity.Set(velArr[v], velArr[v + 1], velArr[v + 2]);
                                        Rigids[r].angularVelocity.Set(velArr[v + 3], velArr[v + 4], velArr[v + 5]);
                                    }
                                }

                                if (e["inputs"] != null)
                                {
                                    var inputs = (PhaseSpace.OWL.Input[])e["inputs"];
                                    foreach (var input in inputs)
                                    {
                                        if (!Inputs.ContainsKey(input.hw_id))
                                        {
                                            byte[] hwBytes = System.BitConverter.GetBytes(input.hw_id);
                                            if (hwBytes[7] == 0x78 && hwBytes[6] == 0x62 && hwBytes[5] == 0x65 && hwBytes[4] == 0x65)
                                            {
                                                var xbee = new XBeeInput(input);
                                                Inputs.Add(input.hw_id, xbee);
                                                XBeeInputs.Add(xbee.RadioAddress, xbee);
                                            }
                                            else
                                            {
                                                Inputs.Add(input.hw_id, new Input(input));
                                            }

                                            Debug.Log("[OWL] Adding input: 0x" + input.hw_id.ToString("x32").TrimStart('0'));

                                        }

                                        Inputs[input.hw_id].Update(input);
                                    }
                                }

                                if (e["driverstatus"] != null)
                                {
                                    var inputs = (PhaseSpace.OWL.Input[])e["driverstatus"];

                                    foreach (var input in inputs)
                                    {
                                        if (!Drivers.ContainsKey(input.hw_id))
                                        {
                                            if (e.time - input.time <= Frequency)
                                            {
                                                Drivers[input.hw_id] = new Driver(input.hw_id);
                                                Drivers[input.hw_id].Update(input.flags);
                                            }
                                        }
                                        else
                                        {
                                            Drivers[input.hw_id].Update(input.flags);
                                        }
                                    }
                                }

                                //usually internal frames, not buffered
                                if (e["peaks"] != null)
                                {
                                    if (OnReceivedPeaks != null)
                                        OnReceivedPeaks((PhaseSpace.OWL.Peak[])e["peaks"]);
                                }
                                break;
                            case Type.CAMERA:
                                if (e.name == "cameras")
                                {
                                    //Debug.Log("[CAMERA] Cameras");
                                    foreach (var cam in ((OWL.Camera[])e.data))
                                    {
                                        if (cameraIDLookup.ContainsKey(cam.id))
                                        {
                                            cameraIDLookup[cam.id].Update(cam);
                                        }
                                        else
                                        {
                                            var tempCam = new Camera(cam);
                                            //Debug.Log("[CAMERA]  Camera: " + tempCam.id);
                                            cameraIDLookup.Add(cam.id, tempCam);
                                            Cameras.Add(tempCam);
                                            Cameras.Sort((a, b) => a.id.CompareTo(b.id));
                                        }
                                    }

                                }
                                else if (e.name == "missingcameras")
                                {
                                    //Debug.Log("[CAMERA] Missing Cameras");
                                    foreach (var cam in ((OWL.Camera[])e.data))
                                    {
                                        if (missingCameraIDLookup.ContainsKey(cam.id))
                                        {
                                            missingCameraIDLookup[cam.id].Update(cam);
                                        }
                                        else
                                        {
                                            var tempCam = new Camera(cam);
                                            //Debug.Log("[CAMERA]  Missing Camera: " + tempCam.id);
                                            missingCameraIDLookup.Add(tempCam.id, tempCam);
                                            MissingCameras.Add(tempCam);
                                            MissingCameras.Sort((a, b) => a.id.CompareTo(b.id));
                                        }
                                    }

                                }
                                break;
                            case Type.DEVICEINFO:
                                foreach (var info in (DeviceInfo[])e.data)
                                {
                                    switch (info.type)
                                    {
                                        case "driver":
                                        case "microdriver":
                                            //ignore whatever this thing is
                                            if (info.hw_id == 0xffffffff)
                                                break;

                                            if (info.status.Contains("inactive=1"))
                                            {
                                                if (Drivers.ContainsKey(info.hw_id))
                                                {
                                                    Debug.Log(string.Format("[OWL] {0} 0x{1} Lost", info.type, info.hw_id.ToString("x2")));
                                                    Drivers.Remove(info.hw_id);
                                                }
                                            }

                                            if (info.time == 0)
                                            {
                                                //ignore
                                            }
                                            else if (!Drivers.ContainsKey(info.hw_id))
                                            {
                                                if (info.status != "" && info.status != "inactive=1")
                                                {
                                                    Debug.Log(string.Format("[OWL] {0} 0x{1} Found", info.type, info.hw_id.ToString("x2")));
                                                    Drivers[info.hw_id] = new Driver(info);
                                                    if (EmbeddedDevices != null)
                                                    {
                                                        EmbeddedDevices.AddDevice(info, true);
                                                    }
                                                }
                                            }
                                            else if (Drivers[info.hw_id].Info != info)
                                            {
                                                Drivers[info.hw_id].Info = info;
                                            }

                                            break;
                                        case "camera":
                                            if (!cameraHWIDLookup.ContainsKey(info.hw_id))
                                            {
                                                bool missing = info.name.StartsWith("missing");
                                                uint id = uint.Parse(missing ? info.name.Replace("missing", "") : info.name);

                                                Camera cam;

                                                if (!missing)
                                                {
                                                    if (cameraIDLookup.ContainsKey(id))
                                                    {
                                                        cam = cameraIDLookup[id];
                                                        cam.Update(info);
                                                    }
                                                    else
                                                    {
                                                        cam = new Camera(info);
                                                        cameraIDLookup.Add(cam.id, cam);
                                                        Cameras.Add(cam);
                                                        Cameras.Sort((a, b) => a.id.CompareTo(b.id));
                                                    }
                                                }
                                                else
                                                {
                                                    if (missingCameraIDLookup.ContainsKey(id))
                                                    {
                                                        cam = missingCameraIDLookup[id];
                                                        cam.Update(info);
                                                    }
                                                    else
                                                    {
                                                        cam = new Camera(info);
                                                        missingCameraIDLookup.Add(cam.id, cam);
                                                        MissingCameras.Add(cam);
                                                        MissingCameras.Sort((a, b) => a.id.CompareTo(b.id));
                                                    }
                                                }

                                                cameraHWIDLookup.Add(info.hw_id, cam);
                                            }
                                            else
                                            {
                                                //TODO:  Update devices arbitrarily
                                                //cameraHWIDLookup[info.hw_id].Update(info);
                                            }
                                            break;
                                        default:

                                            break;
                                    }

                                }

                                if (OnReceivedDeviceInfo != null)
                                    OnReceivedDeviceInfo(e);
                                break;
                        }
                    }
                }
            }
        }

        //TODO:  Implement save calibrated power
        /// <summary>
        /// Sets system LED Power
        /// </summary>
        /// <param name="pwr"></param>
        public void SetPower(float pwr)
        {
            if (SlaveMode == SlaveMode.Master && State >= ConnectionState.Initialized)
            {
                LEDPower = pwr;
                context.options("system.LEDPower=" + pwr.ToString("f2"));
            }

        }
        #endregion

        #region Device Management
        /// <summary>
        /// Force encodes listed devices by HWID
        /// Drivers, Microdrivers
        /// </summary>
        /// <param name="hwids"></param>
        public void EncodeDevices(params ulong[] hwids)
        {
            if (SlaveMode == SlaveMode.Master && State >= ConnectionState.Initialized)
            {
                string str = "";
                foreach (var hwid in hwids)
                {
                    str += "0x" + hwid.ToString("x2") + ",";
                }
                str = str.Remove(str.Length - 1);
                context.options("system.encode=" + str);
            }
        }

        /// <summary>
        /// Pushes Device pairing information to OWL Server
        /// </summary>
        public void UpdateDevices()
        {
            if (EmbeddedDevices == null)
                return;

            if (State >= ConnectionState.Open)
            {
                var warnings = EmbeddedDevices.GetWarnings();

                if (warnings != "")
                {
                    Debug.LogWarning("[OWL] " + warnings, EmbeddedDevices);
                }

                context.options("profile.json=" + EmbeddedDevices.GetJSON());
            }

        }

        /// <summary>
        /// Enumerates a device's HWID to a Name in the associated OWLDevices object.
        /// Optionally serializes the data (platform dependent)
        /// </summary>
        /// <param name="hwid"></param>
        /// <param name="name"></param>
        /// <param name="serialize"></param>
        public void EnumerateDevice(ulong hwid, string name, bool serialize = false)
        {
            if (EmbeddedDevices == null)
                return;

            EmbeddedDevices.Enumerate(hwid, name, serialize);
        }
        #endregion

        #region Tracker Management
        /// <summary>
        /// Assigns Markers to a Tracker
        /// </summary>
        /// <param name="trackerId"></param>
        /// <param name="leds"></param>
        /// <param name="points"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public bool AssignMarkers(uint trackerId, uint[] leds, Vector3[] points, string[] names)
        {
            MarkerInfo[] markers = new MarkerInfo[leds.Length];

            if (names == null || names.Length != markers.Length)
                names = new string[markers.Length];

            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = new MarkerInfo(leds[i], trackerId, names[i], string.Format("pos={0},{1},{2}", points[i].x, points[i].y, points[i].z));
            }


            if (markers.Length > 0)
            {
                return context.assignMarkers(markers);
            }

            return false;
        }


        /// <summary>
        /// Creates a Rigid Tracker.  Points array must be in PhaseSpace Coordinates in real-world millimeter units.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="markers"></param>
        /// <param name="points"></param>
        /// <param name="options"></param>
        public void CreateRigidTracker(uint id, string name, uint[] markers, Vector3[] points, string[] markerNames = null, string options = "")
        {
            if (SlaveMode == SlaveMode.Slave)
                return;

            if (markerNames == null || markerNames.Length != markers.Length)
                markerNames = new string[markers.Length];

            var info = context.trackerInfo(id);
            if (info != null)
            {
                if (info.type == "rigid")
                {
                    DestroyRigidTracker(id);
                }
            }

            if (!context.createTracker(id, "rigid", name, options))
                throw new System.Exception("Cannot create tracker!");

            if (!AssignMarkers(id, markers, points, markerNames))
                throw new System.Exception("Cannot assign markers!");
        }

        /// <summary>
        /// Creates a Calibration Tracker.  Points array must be in PhaseSpace Coordinates in real-world millimeter units.
        /// Generally used as a Communication object during persistently stored server options twiddling.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="markers"></param>
        /// <param name="points"></param>
        /// <param name="markerNames"></param>
        /// <param name="options"></param>
        public void CreateCalibrationTracker(uint id, string name, uint[] markers, Vector3[] points, string[] markerNames = null, string options = "")
        {
            if (SlaveMode == SlaveMode.Slave)
                return;

            if (markerNames == null || markerNames.Length != markers.Length)
            {
                if (markers != null)
                    markerNames = new string[markers.Length];
            }

            if (!context.createTracker(id, "calibration", name, options))
                throw new System.Exception("Cannot create tracker!");

            if (markers != null && markers.Length > 0)
            {
                if (!AssignMarkers(id, markers, points, markerNames))
                    throw new System.Exception("Cannot assign markers!");
            }

        }

        //destroy rigid tracker and return markers to default tracker unless forced
        public void DestroyRigidTracker(uint id, bool force = false)
        {
            if (SlaveMode == SlaveMode.Slave)
                return;

            if (force)
            {
                context.destroyTracker(id);
                return;
            }

            var defaultInfo = context.trackerInfo(0);
            if (defaultInfo == null || defaultInfo.type != "point")
            {
                return;
            }

            var info = context.trackerInfo(id);
            if (info == null)
                return;

            if (info.type != "rigid")
            {
                Debug.LogWarning("[OWL] Trying to destroy Rigid Tracker " + id + " but it is of type " + info.type);
                return;
            }
            context.destroyTracker(id);

            MarkerInfo[] infos = new MarkerInfo[info.marker_ids.Length];
            for (int i = 0; i < infos.Length; i++)
                infos[i] = new MarkerInfo(info.marker_ids[i], 0);

            context.assignMarkers(infos);

        }

        /// <summary>
        /// Nuke a tracker from orbit.  Indiscriminately.
        /// </summary>
        /// <param name="id"></param>
        public void DestroyTracker(uint id)
        {
            if (SlaveMode == SlaveMode.Slave)
                return;

            context.destroyTracker(id);
        }

        /// <summary>
        /// Creates a Rigid Tracker from an OWLRigidData object.
        /// </summary>
        /// <param name="data"></param>
        public void CreateRigidTracker(OWLRigidData data)
        {
            CreateRigidTracker(data.trackerId, data.trackerName, data.ids, data.points, data.names, data.options);
        }

        /// <summary>
        /// Creates a Calibration Tracker from an OWLRigidData object.
        /// </summary>
        /// <param name="data"></param>
        public void CreateCalibrationTracker(OWLRigidData data)
        {
            CreateCalibrationTracker(data.trackerId, data.trackerName, data.ids, data.points, data.names, data.options);
        }

        /// <summary>
        /// Sets Tracker Options by Tracker ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="opts"></param>
        public void SetTrackerOptions(uint id, string opts)
        {
            context.trackerOptions(id, opts);
        }
        #endregion

        #region Gizmos
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (State >= ConnectionState.Initialized)
            {
                if (MarkerGizmos)
                {
                    OWLEditorUtilities.DrawMarkers(Markers, transform);
                }

                if (RigidGizmos)
                {
                    OWLEditorUtilities.DrawRigids(Rigids, transform);
                }

                if (CameraGizmos)
                {
                    OWLEditorUtilities.DrawCameras(this);
                }

            }
        }
#endif
        #endregion
    }
}

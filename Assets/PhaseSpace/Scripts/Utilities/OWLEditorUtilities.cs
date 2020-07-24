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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using PhaseSpace.Unity;


namespace PhaseSpace.Unity
{
    public class HWID : PropertyAttribute
    {
        public HWID()
        {
            //do nothing
        }
    }

    public static class OWLEditorUtilities
    {
        /// <summary>
        /// Helper class to query the system-level status of a PhaseSpace server.
        /// Does not require an OWLClient to be initialized.
        /// </summary>
        public static class ServerStatus
        {
            class ServerStatusWebClient : WebClient
            {
                protected override WebRequest GetWebRequest(Uri address)
                {
                    var w = base.GetWebRequest(address);
                    w.Timeout = 1000;
                    return w;
                }
            }
            static ServerStatusWebClient systemStatusWebClient = new ServerStatusWebClient();

            public static string OWLDLog(string hostname, int length = -1)
            {
                string response = systemStatusWebClient.DownloadString(SystemStatusURI(hostname, "owldlog", length));
                try
                {
                    var table = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    var result = table["result"] as Dictionary<string, object>;
                    return result["owldlog"] as string;
                }
                catch
                {

                }

                return response;
            }

            public static string Packages(string hostname)
            {

                string response = systemStatusWebClient.DownloadString(SystemStatusURI(hostname, "packages"));
                try
                {
                    var table = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    var result = table["result"] as Dictionary<string, object>;
                    return MiniJSON.Json.Serialize(result["packages"]);
                }
                catch
                {

                }

                return response;
            }

            public static string Network(string hostname)
            {

                string response = systemStatusWebClient.DownloadString(SystemStatusURI(hostname, "network"));
                try
                {
                    var table = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    var result = table["result"] as Dictionary<string, object>;
                    return MiniJSON.Json.Serialize(result["network"]);
                }
                catch
                {

                }

                return response;
            }

            public static string Storage(string hostname)
            {

                string response = systemStatusWebClient.DownloadString(SystemStatusURI(hostname, "storage"));
                try
                {
                    var table = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    var result = table["result"] as Dictionary<string, object>;
                    return MiniJSON.Json.Serialize(result["storage"]);
                }
                catch
                {

                }

                return response;
            }

            public static string CPU(string hostname)
            {

                string response = systemStatusWebClient.DownloadString(SystemStatusURI(hostname, "cpu"));
                try
                {
                    var table = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    var result = table["result"] as Dictionary<string, object>;
                    return MiniJSON.Json.Serialize(result["cpu"]);
                }
                catch
                {

                }

                return response;
            }

            public static string Memory(string hostname)
            {

                string response = systemStatusWebClient.DownloadString(SystemStatusURI(hostname, "meminfo"));
                try
                {
                    var table = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    var result = table["result"] as Dictionary<string, object>;
                    return MiniJSON.Json.Serialize(result["meminfo"]);
                }
                catch
                {

                }

                return response;
            }

            public static string NetLink(string hostname)
            {

                string response = systemStatusWebClient.DownloadString(SystemStatusURI(hostname, "netlink"));
                try
                {
                    var table = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
                    var result = table["result"] as Dictionary<string, object>;
                    return MiniJSON.Json.Serialize(result["netlink"]);
                }
                catch
                {

                }

                return response;
            }

            //packages,owldlog,network,storage,cpu,netlink,meminfo,all
            static string SystemStatusURI(string hostname, string action, int length = -1)
            {
                return string.Format("http://{0}/public/info.php?action={1}{2}", hostname, action, length < 0 ? "" : "&length=" + length);
            }
        }


        public static Color[] SlotColors;

        static OWLEditorUtilities()
        {
            SlotColors = new Color[16];
            SlotColors[0] = Color.red;
            SlotColors[1] = Color.green;
            SlotColors[2] = Color.blue;
            SlotColors[3] = Color.yellow;
            SlotColors[4] = new Color(0, 1, 1, 1);
            SlotColors[5] = new Color(1, 0, 1, 1);
            SlotColors[6] = new Color(1, 0.5f, 0, 1);
            SlotColors[7] = new Color(0.5f, 1, 0, 1);
            SlotColors[8] = new Color(0, 1, 0.5f, 1);
            SlotColors[9] = new Color(0, 0.5f, 1, 1);
            SlotColors[10] = new Color(0.5f, 0, 1, 1);
            SlotColors[11] = new Color(1, 0, 0.5f, 1);
        }

        public static string GetByteArrayString(byte[] bytes)
        {
            string s = "";
            foreach (var b in bytes)
            {
                s += b.ToString("x2") + " ";
            }

            return s;
        }

        public static void DrawMarkers(List<Marker> Markers, Transform origin = null)
        {
#if UNITY_EDITOR
            Vector3 size = Vector3.one * 0.01f;
            foreach (var m in Markers)
            {
                if (m.Condition <= TrackingCondition.Invalid)
                    continue;
                Vector3 pos = m.position;
                Vector3 vel = m.velocity;

                if (origin != null)
                {
                    pos = origin.TransformPoint(pos);
                    vel = origin.TransformDirection(vel);
                }

                Handles.color = SlotColors[m.Slot];
                Handles.DrawWireCube(pos, size);
                Handles.color = Color.magenta;
                Handles.DrawLine(pos, pos + vel);
                Handles.Label(pos, m.id.ToString()/* + "\r\nCondition: " + m.cond.ToString("f0")*/);
            }

#endif
        }

        public static void DrawMarkers(Vector3[] Positions, uint[] Ids, Transform origin = null)
        {
#if UNITY_EDITOR
            Vector3 size = Vector3.one * 0.01f;
            for (int i = 0; i < Positions.Length; i++)
            {

                //if (m.Condition <= TrackingCondition.Invalid)
                //    continue;
                Vector3 pos = Positions[i];
                if (origin != null)
                    pos = origin.TransformPoint(pos);

                Handles.color = SlotColors[0];
                Handles.DrawWireCube(pos, size);
                Handles.Label(pos, Ids[i].ToString());
            }

#endif
        }

        public static void DrawAxes(Vector3 pos, Quaternion rot)
        {
#if UNITY_EDITOR
            Handles.color = Color.blue;
            Handles.DrawLine(pos, pos + (rot * Vector3.forward) * 0.15f);
            Handles.color = Color.red;
            Handles.DrawLine(pos, pos + (rot * Vector3.right) * 0.15f);
            Handles.color = Color.green;
            Handles.DrawLine(pos, pos + (rot * Vector3.up) * 0.15f);
#endif
        }

        public static void DrawRigids(List<Rigid> Rigids, Transform origin = null)
        {
#if UNITY_EDITOR
            foreach (var r in Rigids)
            {
                if (r.Condition <= TrackingCondition.Invalid)
                    continue;

                if (origin != null)
                {
                    Vector3 pos = origin.TransformPoint(r.position);
                    Vector3 vel = origin.TransformDirection(r.velocity);
                    DrawAxes(pos, origin.rotation * r.rotation);
                    Handles.color = Color.magenta;
                    Handles.DrawLine(pos, pos + (vel * 1));
                }
                else
                {
                    DrawAxes(r.position, r.rotation);
                }

            }
#endif
        }

        //TODO:  Abstract to collection of cameras?
        public static void DrawCameras(OWLClient owl)
        {
#if UNITY_EDITOR
            foreach (var c in owl.Cameras)
            {
                if (c == null)
                    break;

                DrawCamera(owl, c);
            }

            foreach (var c in owl.MissingCameras)
            {
                if (c == null)
                    break;

                DrawCamera(owl, c);
            }
#endif
        }


        static void DrawCamera(OWLClient owl, Camera c)
        {
#if UNITY_EDITOR
            Vector3 a = c.position;
            a.y = 0;
            a = owl.transform.TransformPoint(a);

            Color color = new Color(1, 1, 0, 0.25f);

            if (c.missing)
                color = new Color(1, 0, 0, 0.75f);
            else if (c.Condition == TrackingCondition.Invalid) //not calibrated
                color = new Color(1, 0, 1, 0.75f);

            Vector3 b = owl.transform.TransformPoint(c.position);

            Handles.color = color;
            Handles.DrawWireDisc(a, owl.transform.up, 0.1f);
            Handles.DrawLine(a, b);
            Quaternion rot = owl.transform.rotation * c.rotation;

            Gizmos.color = color;
            Gizmos.DrawWireSphere(b, 0.15f * 0.5f);
            Handles.DrawDottedLine(b, b + (rot * new Vector3(0, 0, 2)), 5);
            Handles.Label(b + (Vector3.up * 0.25f), c.missing ? "Missing" : c.alias);
#endif
        }
    }
}

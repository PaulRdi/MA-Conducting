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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using PhaseSpace.OWL;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PhaseSpace.Unity
{
    public static class OWLScan
    {
        static bool active = false;
        static bool scanning = false;

        public static ServerInfo[] Servers = new ServerInfo[0];

        public static bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (value && !active)
                {
                    active = value;
                    //start thread
                    if (!scanning)
                    {
#if UNITY_EDITOR
                        EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
#endif
                        var thread = new Thread(Scan);
                        thread.Start();
                    }
                }
                else if (!value && active)
                {
                    active = value;
                    //thread will kill itself
                }
            }
        }

        static void OnPlaymodeStateChanged()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying == false)
                active = false;
#endif
        }

        static void Scan()
        {
            scanning = true;
            while (active)
            {
                string[] serverData = null;
                List<ServerInfo> servers = new List<ServerInfo>();

                var scan = new OWL.Scan();

                if (!scan.send("unity"))
                {
                    active = false;
                }
                else
                {
                    serverData = scan.listen();
                }

                if (serverData != null)
                {
                    for (int i = 0; i < serverData.Length; i++)
                    {
                        try
                        {
                            var data = OWL.utils.tomap(serverData[i]);
                            servers.Add(new ServerInfo(data["ip"], serverData[i]));
                        }
                        catch (KeyNotFoundException)
                        {
                            //ip not found in map, ignore
                        }

                    }

                    lock (Servers)
                    {
                        Servers = servers.ToArray();
                    }
                }
                Thread.Sleep(5000);

            }

#if UNITY_EDITOR
            EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
#endif
            scanning = false;
        }
    }
}


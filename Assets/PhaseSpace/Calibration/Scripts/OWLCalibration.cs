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
using PhaseSpace.Unity;

namespace PhaseSpace.Unity
{
    public class OWLCalibration : MonoBehaviour
    {
        public enum CalibrationSpeed { Exhaustive = 0, Fastest = 200, Fast = 400, Normal = 800, Slow = 1200 }
        public enum CalibrationState { None, Ready, AutoPower, Capturing, Calibrating }

        public OWLClient owl;
        public OWLRigidData calibrationObjectData;
        public GameObject cameraFramePrefab;
        public Transform cameraFrameRoot;
        public CalibrationSpeed calibrationSpeed = CalibrationSpeed.Normal;
        CalibrationState state = CalibrationState.None;
        public bool refreshCalibration = false;

        Dictionary<uint, OWLCameraFrame> cameraFrameTable = new Dictionary<uint, OWLCameraFrame>();
        Dictionary<uint, Dictionary<uint, PhaseSpace.OWL.Peak[]>> peakTable;
        bool lastCalibrationSuccessful;
        bool lastCaptureDataSaved;
        bool cleanup = false;
        System.DateTime buttonDebounceTime;

        [ContextMenu("Init Calibration Mode")]
        public void InitCalibrationMode()
        {
            //breakout if not in calibration mode
            if (owl.GetProperty<string>("profile") != "calibration")
                return;

            owl.OnReceivedBytes += OnReceivedBytes;
            peakTable = new Dictionary<uint, Dictionary<uint, PhaseSpace.OWL.Peak[]>>();
            for (int i = 0; i < owl.Cameras.Count; i++)
            {
                var frame = Instantiate<GameObject>(cameraFramePrefab, cameraFrameRoot, false).GetComponent<OWLCameraFrame>();
                frame.cameraId = owl.Cameras[i].id;
                frame.SetName(owl.Cameras[i].alias);
                cameraFrameTable.Add(frame.cameraId, frame);

                uint id = owl.Cameras[i].id;
                peakTable.Add(id, new Dictionary<uint, PhaseSpace.OWL.Peak[]>());
            }


            //TODO:  Sort by Hub Port instead of auto grid crap
            foreach (var pair in cameraFrameTable)
            {
                pair.Value.GetComponent<RectTransform>().SetSiblingIndex((int)pair.Key);
            }

            foreach (var pair in cameraFrameTable)
            {
                pair.Value.GetComponent<RectTransform>().SetSiblingIndex((int)pair.Key);
            }


            if (owl.SlaveMode == SlaveMode.Master)
            {
                owl.SetOption("calibration=" + (refreshCalibration ? 1 : 0));
                owl.CreateCalibrationTracker(calibrationObjectData);
            }
            else
            {
                //slave mode
            }

            owl.OnReceivedPeaks += OnReceivedPeaks;

            state = CalibrationState.Ready;

            OWLMessage.Display("Calibration Mode", false);
        }       

        [ContextMenu("Init Calibration Mode", true)]
        bool ValidateInitCalibrationMode()
        {
            return state == CalibrationState.None && owl.State >= OWLClient.ConnectionState.Initialized;
        }

        [ContextMenu("End Calibration Mode")]
        public void EndCalibrationMode()
        {
            owl.OnReceivedBytes -= OnReceivedBytes;
            owl.OnReceivedPeaks -= OnReceivedPeaks;

            cleanup = true;
            if (owl.SlaveMode == SlaveMode.Master)
            {
                //clean up trackers
                owl.DestroyTracker(calibrationObjectData.trackerId);

                owl.Context.createTracker(0, "point", "default");
            }

            OWLMessage.Display("Calibration Ended", false);
            state = CalibrationState.None;
        }

        void StateControl()
        {
            if (System.DateTime.Now < buttonDebounceTime)
                return;

            foreach (var d in owl.Drivers.Values)
            {
                if (d.Buttons[0])
                {
                    switch (state)
                    {
                        case CalibrationState.Ready:
                            if (!lastCalibrationSuccessful)
                            {
                                StartCapturing();
                            }
                            else
                            {
                                if (lastCaptureDataSaved)
                                {
                                    EndCalibrationMode();
                                }
                                else
                                {
                                    Save();
                                }

                            }

                            buttonDebounceTime = System.DateTime.Now + new System.TimeSpan(0, 0, 1);
                            break;
                        case CalibrationState.Capturing:
                            StopCapturing();
                            buttonDebounceTime = System.DateTime.Now + new System.TimeSpan(0, 0, 1);
                            Calibrate();
                            break;
                    }
                }
            }
        }

        //work around to deal with threaded end of calibration mode
        void Update()
        {
            if (owl.SlaveMode == SlaveMode.Master)
                StateControl();

            if (cleanup)
            {
                ClearCameraFrames();
                cleanup = false;
            }
        }

        void ClearCameraFrames()
        {
            foreach (var f in cameraFrameTable.Values)
                Destroy(f.gameObject);

            cameraFrameTable.Clear();
        }

        [ContextMenu("End Calibration Mode", true)]
        bool ValidateEndCalibrationMode()
        {
            return state == CalibrationState.Ready;
        }

        private void OnReceivedBytes(PhaseSpace.OWL.Event e)
        {
            if (e.type_id == PhaseSpace.OWL.Type.BYTE)
            {
                if (e.name == "calibration")
                {
                    string msg = new string(System.Text.Encoding.UTF8.GetChars((byte[])e.data));
                    //Debug.Log(msg);
                    if (state == CalibrationState.Calibrating)
                    {
                        //check for sucess
                        if (msg.Contains("fail"))
                        {
                            //calibration Failed
                            lastCalibrationSuccessful = false;
                            OWLMessage.Display("Calibration Failed!", false);
                            Debug.LogWarning(msg);
                            state = CalibrationState.Ready;
                        }
                        else if (msg.Contains("finished"))
                        {
                            //calibration 'success'
                            OWLMessage.Display("Calibration Successful.", false);
                            lastCalibrationSuccessful = true;
                            state = CalibrationState.Ready;
                        }
                        else
                        {
                            Debug.Log(msg);
                            OWLMessage.Display(msg, false);
                        }
                    }
                    else
                    {
                        OWLMessage.Display(msg, false);
                    }
                }
            }
        }

        Dictionary<uint, float> peakAmplitudeTable = new Dictionary<uint, float>();
        private void OnReceivedPeaks(PhaseSpace.OWL.Peak[] peaks)
        {
            uint[] markerCounter = new uint[20];

            lock (peakAmplitudeTable)
            {
                peakAmplitudeTable.Clear();

                foreach (var p in peakTable)
                {
                    p.Value.Clear();
                }

                foreach (var c in cameraFrameTable.Values)
                {
                    c.ClearPeaks();
                }

                foreach (var p in peaks)
                {
                    //reject unknown markers
                    if (p.id == uint.MaxValue)
                        continue;

                    if (cameraFrameTable.ContainsKey(p.camera))
                    {
                        cameraFrameTable[p.camera].UpdatePeak(p);
                    }

                    //if (capturing)
                    //{
                    if (peakTable[p.camera].ContainsKey(p.id))
                    {
                        peakTable[p.camera][p.id][p.detector] = p;
                        if (peakTable[p.camera][p.id][0] != null && peakTable[p.camera][p.id][1] != null)
                        {
                            //valid 2-detector peak
                            markerCounter[(int)p.id]++;

                            var a = peakTable[p.camera][p.id][0];
                            var b = peakTable[p.camera][p.id][1];

                            if (!peakAmplitudeTable.ContainsKey(a.id))
                                peakAmplitudeTable.Add(a.id, (a.amp + b.amp) * 0.5f);
                            else
                            {
                                float amp = peakAmplitudeTable[a.id];
                                amp = (amp + ((a.amp + b.amp) * 0.5f)) * 0.5f;
                                peakAmplitudeTable[a.id] = amp;
                            }

                        }
                    }
                    else
                    {
                        peakTable[p.camera][p.id] = new PhaseSpace.OWL.Peak[2];
                        peakTable[p.camera][p.id][p.detector] = p;
                    }
                    //}
                }
            }


            if (state == CalibrationState.Capturing)
            {
                for (int i = 0; i < markerCounter.Length; i++)
                {
                    if (markerCounter[i] >= 2)
                    {
                        //seen by at least 2 cameras
                        foreach (var c in cameraFrameTable.Values)
                        {
                            if (peakTable[c.cameraId].ContainsKey((uint)i))
                            {
                                if (peakTable[c.cameraId][(uint)i][0] != null && peakTable[c.cameraId][(uint)i][1] != null)
                                    c.PaintCalibrationHistory(peakTable[c.cameraId][(uint)i]);
                            }
                        }
                    }
                }
            }
        }

        public float amplitudeThreshhold = 10000;
        IEnumerator AutoAdjustPowerRoutine()
        {
            state = CalibrationState.AutoPower;
            OWLMessage.Display("Auto LED Power", false);
            float originalPower = float.Parse(owl.GetOption("system.LEDPower"));

            float pow = 0.01f;

            owl.SetOption("system.LEDPower=" + pow.ToString("f2"), true);
            bool foundPower = false;

            yield return new WaitForSeconds(0.5f);

            lock (peakAmplitudeTable)
            {
                peakAmplitudeTable.Clear();
            }

            int consecutiveGoodFrames = 0;
            while (pow < 1)
            {
                owl.SetOption("system.LEDPower=" + pow.ToString("f2"), true);
                yield return new WaitForSeconds(0.1f);

                lock (peakAmplitudeTable)
                {
                    int goodPeakCount = 0;

                    foreach (var amp in peakAmplitudeTable.Values)
                    {
                        if (amp > amplitudeThreshhold)
                            goodPeakCount++;
                    }

                    if (goodPeakCount > (calibrationObjectData.ids.Length / 2))
                    {
                        consecutiveGoodFrames++;

                        if (consecutiveGoodFrames == 10)
                        {
                            pow = Mathf.MoveTowards(pow, 1, 0.1f);
                            foundPower = true;
                            break;
                        }

                    }
                    else
                    {
                        consecutiveGoodFrames = 0;
                        pow += 0.01f;
                    }
                }

            }

            if (!foundPower)
            {
                pow = originalPower;
                OWLMessage.Display("Failed!\nLED Power:   " + pow);
            }
            else
            {
                OWLMessage.Display("LED Power:   " + pow);
            }

            owl.SetOption("system.LEDPower=" + pow.ToString("f2"));

            state = CalibrationState.Ready;

        }


        [ContextMenu("Auto Adjust Power")]
        public void AutoAdjustPower()
        {
            StartCoroutine(AutoAdjustPowerRoutine());
        }

        [ContextMenu("Auto Adjust Power", true)]
        bool ValidateAutoPower()
        {
            return state == CalibrationState.Ready;
        }

        [ContextMenu("Start")]
        public void StartCapturing()
        {
            lastCaptureDataSaved = false;
            OWLMessage.Display("Capturing", false);
            owl.SetTrackerOptions(calibrationObjectData.trackerId, "calibrate=start");
            state = CalibrationState.Capturing;
        }

        [ContextMenu("Start", true)]
        bool ValidateStart()
        {
            return state == CalibrationState.Ready;
        }

        [ContextMenu("Stop")]
        public void StopCapturing()
        {
            OWLMessage.Display("Done Capturing", false);
            owl.SetTrackerOptions(calibrationObjectData.trackerId, "calibrate=stop");
            state = CalibrationState.Ready;
        }

        [ContextMenu("Stop", true)]
        bool ValidateStop()
        {
            return state == CalibrationState.Capturing;
        }

        [ContextMenu("Calibrate")]
        public void Calibrate()
        {
            lastCalibrationSuccessful = false;
            state = CalibrationState.Calibrating;
            OWLMessage.Display("Calibrating", false);
            owl.SetOption("calibration=" + (refreshCalibration ? 1 : 0));
            //touch markers again because reasons
            owl.AssignMarkers(calibrationObjectData.trackerId, calibrationObjectData.ids, calibrationObjectData.points, calibrationObjectData.names);
            owl.SetTrackerOptions(calibrationObjectData.trackerId, "calibrate=calibrate," + (int)calibrationSpeed);
        }

        [ContextMenu("Calibrate", true)]
        bool ValidateCalibrate()
        {
            //TODO: Ensure some samples were collected
            return state == CalibrationState.Ready;
        }

        [ContextMenu("Save")]
        public void Save()
        {
            owl.SetTrackerOptions(calibrationObjectData.trackerId, "calibrate=save");
            lastCaptureDataSaved = true;
            OWLMessage.Display("Calibration Saved", false);
        }

        [ContextMenu("Save", true)]
        bool ValidateSave()
        {
            return state == CalibrationState.Ready;
        }
    }
}
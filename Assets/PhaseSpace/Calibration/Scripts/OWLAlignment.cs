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

public class OWLAlignment : MonoBehaviour
{
    public enum AlignmentState { None, Ready, Snapshots, Rigid }
    public OWLClient owl;
    public OWLRigidData alignmentObjectData;
    AlignmentState state;


    List<Vector3> positions = new List<Vector3>();
    System.DateTime buttonDebounceTime;

    [ContextMenu("Init Alignment Mode")]
    public void InitAlignmentMode()
    {

        if (owl.SlaveMode == SlaveMode.Master)
        {
            //breakout if not in calibration mode
            if (owl.GetProperty<string>("profile") != "calibration")
                return;

            owl.SetOption("calibration=1");

            //purge all trackers just in case
            var trackerIds = owl.Context.property<uint[]>("trackers");
            foreach (var t in trackerIds)
            {
                owl.DestroyTracker(t);
            }
            //create calibration tracker to facilitate serialization
            owl.CreateCalibrationTracker(0, "communication", null, null);
            //forcefully use alignment data as tracker 1
            owl.CreateRigidTracker(1, alignmentObjectData.trackerName, alignmentObjectData.ids, alignmentObjectData.points, alignmentObjectData.names, alignmentObjectData.options);

            state = AlignmentState.Ready;
        }

    }

    [ContextMenu("Init Alignment Mode", true)]
    bool ValidateStartAlignmentMode()
    {
        if (owl != null && state == AlignmentState.None && owl.State >= OWLClient.ConnectionState.Initialized)
            return true;

        return false;
    }

    [ContextMenu("End Alignment Mode")]
    public void EndAlignmentMode()
    {
        owl.DestroyTracker(1);
        owl.DestroyTracker(0);
        //recreate default point tracker just in case
        owl.Context.createTracker(0, "point", "default");

        state = AlignmentState.None;
    }

    [ContextMenu("End Alignment Mode", true)]
    bool ValidateEndAlignmentMode()
    {
        if (state == AlignmentState.None)
            return false;

        return true;
    }

    [ContextMenu("Start Snapshot Mode")]
    public void StartSnapshots()
    {
        if (state != AlignmentState.Ready)
            return;

        state = AlignmentState.Snapshots;
        OWLMessage.Display("Snapshot Mode", false);
    }

    [ContextMenu("Start Snapshot Mode", true)]
    bool ValidateStartSnapshots()
    {
        if (state == AlignmentState.None)
            return false;

        return true;
    }

    //[ContextMenu("Take Snapshot")]
    public void TakeSnapshot()
    {
        if (state != AlignmentState.Snapshots)
            return;

        if (positions.Count == 3)
        {
            positions.Clear();
            //return;
        }


        positions.Add(owl.Rigids[1].position);
        buttonDebounceTime = System.DateTime.Now + new System.TimeSpan(0, 0, 1);

        if (positions.Count == 3)
        {
            Align();
            //positions.Clear();
        }
        else
        {
            OWLMessage.Display("Snapshot " + positions.Count, false);
        }
    }

    [ContextMenu("Start Rigidbody Mode")]
    public void StartRigidAlign()
    {
        if (state == AlignmentState.None)
            return;

        positions.Clear();

        state = AlignmentState.Rigid;
        OWLMessage.Display("Rigidbody Mode", false);
    }

    [ContextMenu("Start Rigidbody Mode", true)]
    bool ValidateStartRigidAlign()
    {
        if (state == AlignmentState.None)
            return false;

        return true;
    }

    [ContextMenu("Reset Pose")]
    public void ResetPose()
    {
        if (state == AlignmentState.None)
            return;
        owl.Context.pose(OWLConversion.Pose(Vector3.zero, Quaternion.identity));
    }

    [ContextMenu("Reset Pose", true)]
    bool ValidateResetPose()
    {
        if (state == AlignmentState.None)
            return false;

        return true;
    }


    //bool tick = false;
    public void RigidAlign()
    {
        if (state != AlignmentState.Rigid)
            return;

        buttonDebounceTime = System.DateTime.Now + new System.TimeSpan(0, 0, 1);

        //if (tick)
        //{
        //    owl.Context.pose(OWLConversion.Pose(Vector3.zero, Quaternion.identity));
        //    tick = false;

        //    OWLMessage.Display("Alignment Reset", false);
        //}
        //else
        //{

        Vector3 posePos = OWLConversion.PosePosition(owl.Context.pose());
        Quaternion poseRot = OWLConversion.PoseRotation(owl.Context.pose());

        owl.Context.pose(OWLConversion.Pose(owl.Rigids[1].position - posePos, Quaternion.Inverse(poseRot) * owl.Rigids[1].rotation));
        //tick = true;

        OWLMessage.Display("Aligned", false);
        //}


    }

    //[ContextMenu("Align")]
    public void Align()
    {
        if (positions.Count != 3)
        {
            return;
        }

        Vector3 origin = positions[0];
        Plane plane = new Plane(positions[0], positions[1], positions[2]);
        Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation((positions[2] - positions[0]).normalized, plane.normal));
        float[] pose = OWLConversion.Pose(-(rotation * origin), rotation);
        owl.Context.pose(pose);
        OWLMessage.Display("Aligned", false);
        positions.Clear();
    }

    [ContextMenu("Save")]
    void Save()
    {
        if (state == AlignmentState.None)
            return;

        owl.SetTrackerOptions(0, "calibrate=setpose");
        owl.SetTrackerOptions(0, "calibrate=save");

        //recenter pose
        owl.Context.pose(OWLConversion.Pose(Vector3.zero, Quaternion.identity));
        OWLMessage.Display("Alignment Saved", false);
    }

    [ContextMenu("Save", true)]
    bool ValidateSave()
    {
        if (state == AlignmentState.None)
            return false;

        return true;
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
                    case AlignmentState.Snapshots:
                        TakeSnapshot();
                        break;
                    case AlignmentState.Rigid:
                        RigidAlign();
                        break;
                }
            }
        }
    }
    private void Update()
    {
        if (owl.SlaveMode == SlaveMode.Master)
        {
            StateControl();
        }
    }

    void OnDrawGizmos()
    {
        if (owl != null)
        {
            if (state == AlignmentState.Snapshots)
            {
                Gizmos.color = Color.green;
                foreach (var pos in positions)
                    Gizmos.DrawWireCube(owl.transform.TransformPoint(pos), Vector3.one * 0.01f);
            }
        }
    }
}

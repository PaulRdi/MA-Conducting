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


    public class OWLRigidbody : MonoBehaviour
    {

        public OWLRigidData rigidData;
        public int id;
        public TrackingCondition minCondition = TrackingCondition.Poor;
        public OWLClient owl;
        public Space space = Space.Self;

        private void Start()
        {
            if (owl == null)
                owl = FindObjectOfType<OWLClient>();
        }
        void Update()
        {
            if (owl == null || !owl.Ready)
                return;

            var r = owl.Rigids[rigidData ? (int)rigidData.trackerId : id];

            if (r.Condition >= minCondition)
            {
                if (space == Space.Self)
                {
                    transform.localPosition = r.position;
                    transform.localRotation = r.rotation;
                }
                else
                {
                    transform.position = owl.transform.TransformPoint(r.position);
                    transform.rotation = owl.transform.rotation * r.rotation;
                }
            }
            else
            {
                //doesn't meet condition requirements
            }

        }
    }
}
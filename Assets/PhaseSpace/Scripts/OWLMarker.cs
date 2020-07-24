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
    public class OWLMarker : MonoBehaviour
    {
        //TODO smart range based on profile
        public int id;
        public TrackingCondition minCondition = TrackingCondition.Poor;
        public OWLClient owl;
        public Space space;

        void Update()
        {
            if (owl == null || !owl.Ready)
                return;

            var m = owl.Markers[id];

            if (m.Condition >= minCondition)
            {
                if (space == Space.Self)
                {
                    transform.localPosition = m.position;
                }
                else
                {
                    transform.position = owl.transform.TransformPoint(m.position);
                }
            }

        }
    }
}
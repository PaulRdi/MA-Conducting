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
    public class OWLRigidLoader : MonoBehaviour
    {
        public OWLClient owl;
        public bool createTrackersOnEnable = false;
        public OWLRigidData[] rigidData;



        void OnOWLInitialized()
        {
            if (createTrackersOnEnable)
                return;

            if (this.enabled == false)
                return;

            CreateTrackers();
        }

        void OnEnable()
        {
            StartCoroutine(WaitUntilReady());
        }

        IEnumerator WaitUntilReady()
        {
            if (!createTrackersOnEnable || owl == null)
                yield break;

            while (!owl.Ready)
                yield return null;

            CreateTrackers();
        }

        void CreateTrackers()
        {
            if (owl == null || !owl.Ready)
                return;

            foreach (var d in rigidData)
                owl.CreateRigidTracker(d);
        }
    }
}
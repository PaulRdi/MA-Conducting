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
using UnityEngine.UI;

//TODO:  Deal with Unity's halted context menus
namespace PhaseSpace.Unity
{
    public class OWLMessage : MonoBehaviour
    {
        public static void Display(object msg, bool immediate = true)
        {
            Display(msg.ToString(), immediate);
        }

        public static void Display(string msg, bool immediate = true)
        {
            if (instance == null)
            {
                Debug.LogWarning(msg);
                return;
            }


            if (immediate)
            {
                try
                {
                    //instance.StopAllCoroutines();
                    instance.queuedMessage = "";
                    instance.StartCoroutine(instance.DisplayRoutine(msg));
                    if (instance.console)
                        Debug.Log(msg);
                }
                catch
                {
                    instance.queuedMessage = msg;
                }

            }
            else
            {
                instance.queuedMessage = msg;
            }
        }

        static OWLMessage instance;

        public AnimationCurve scaleCurve;
        public Text messageField;
        public bool console;

        CanvasGroup group;
        float fadeStartTime = 0;
        string queuedMessage = "";

        void Start()
        {
            instance = this;
            group = GetComponent<CanvasGroup>();
        }

        IEnumerator DisplayRoutine(string msg)
        {
            fadeStartTime = Time.time + 1f;
            group.alpha = 1;
            messageField.text = msg;

            float start = Time.time;
            float end = start + scaleCurve.keys[scaleCurve.keys.Length - 1].time;

            float t = Time.time;

            while (t < end)
            {
                t = Time.time;
                transform.localScale = Vector3.one * scaleCurve.Evaluate(Mathf.InverseLerp(start, end, t));
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (group.alpha > 0 && Time.time > fadeStartTime)
                group.alpha -= Time.deltaTime;

            if (queuedMessage != "")
                Display(queuedMessage);
        }
    }
}
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
using UnityEditor;

namespace PhaseSpace.Unity
{


    [CustomPropertyDrawer(typeof(HWID), true)]
    public class HWIDDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ulong val = System.Convert.ToUInt64(property.longValue);
            position.width -= 120;

            string str = "";
            string preString = "";

            str = "0x" + (val.ToString("x32").TrimStart('0').PadLeft(1, '0').ToUpper());
            preString = str;

            str = EditorGUI.TextField(position, "HWID", str);

            if (str != preString)
            {
                bool parsed = ulong.TryParse(str.TrimStart('0', 'x'), System.Globalization.NumberStyles.HexNumber, null, out val);

                if (parsed)
                {
                    property.longValue = System.Convert.ToInt64(val);
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    if (str == "0" || str == "0x0")
                    {
                        property.longValue = 0;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            position.x += position.width;
            position.width = 120;

            EditorGUI.LabelField(position, str);
        }
    }
}
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
    public static class PhaseSpaceStyles
    {
        public static GUIStyle SlotLabel;
        public static GUIStyle Microdriver;
        public static GUIStyle LEDToggle;
        public static GUIStyle LEDOn;
        public static GUIStyle LEDOff;
        public static GUIStyle MissingMarker;
        public static GUIStyle GiantSceneViewLabel;
        public static Color[] SlotColors;
        public static string[] SlotColorStrings;

        static PhaseSpaceStyles()
        {
            SlotLabel = new GUIStyle(EditorStyles.label);
            SlotLabel.richText = true;
            SlotLabel.alignment = TextAnchor.UpperCenter;
            SlotLabel.fixedHeight = 0;
            SlotLabel.stretchHeight = true;
            SlotLabel.clipping = TextClipping.Overflow;

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

            SlotColorStrings = new string[SlotColors.Length];
            for (int i = 0; i < SlotColors.Length; i++)
            {
                SlotColorStrings[i] = "#" +
                    ((int)(SlotColors[i].r * 0xFF)).ToString("x2") +
                    ((int)(SlotColors[i].g * 0xFF)).ToString("x2") +
                    ((int)(SlotColors[i].b * 0xFF)).ToString("x2") +
                    ((int)(SlotColors[i].a * 0xFF)).ToString("x2");
            }

            Microdriver = new GUIStyle();
            Microdriver.fixedWidth = 134;
            Microdriver.fixedHeight = 54;
            Microdriver.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhaseSpace/Editor/Textures/PS_Microdriver_Off.png");
            Microdriver.normal.textColor = Color.white;
            Microdriver.hover.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhaseSpace/Editor/Textures/PS_Microdriver_On.png");
            Microdriver.hover.textColor = Color.white;
            Microdriver.alignment = TextAnchor.MiddleCenter;
            Microdriver.contentOffset = new Vector2(0, -2);
            Microdriver.clipping = TextClipping.Clip;


            LEDToggle = new GUIStyle();
            LEDToggle.fixedWidth = 19;
            LEDToggle.fixedHeight = 21;
            LEDToggle.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhaseSpace/Editor/Textures/PS_LED_Off.png");
            LEDToggle.onNormal.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhaseSpace/Editor/Textures/PS_LED_On.png");
            LEDToggle.normal.textColor = Color.white;
            LEDToggle.alignment = TextAnchor.UpperCenter;
            LEDToggle.contentOffset = new Vector2(0, 0);

            LEDOn = new GUIStyle();
            LEDOn.fixedWidth = 19;
            LEDOn.fixedHeight = 21;
            LEDOn.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhaseSpace/Editor/Textures/PS_LED_On.png");
            LEDOn.normal.textColor = Color.white;
            LEDOn.fontSize = 10;
            LEDOn.alignment = TextAnchor.UpperCenter;
            LEDOn.fontStyle = FontStyle.Bold;
            LEDOn.contentOffset = new Vector2(0, 21);
            LEDOn.richText = true;

            LEDOff = new GUIStyle();
            LEDOff.fixedWidth = 19;
            LEDOff.fixedHeight = 21;
            LEDOff.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhaseSpace/Editor/Textures/PS_LED_Off.png");
            LEDOff.normal.textColor = Color.white;
            LEDOff.fontSize = 10;
            LEDOff.alignment = TextAnchor.UpperCenter;
            LEDOn.fontStyle = FontStyle.Bold;
            LEDOff.contentOffset = new Vector2(0, 21);

            MissingMarker = new GUIStyle(EditorStyles.miniLabel);
            MissingMarker.normal.textColor = Color.red;

            GiantSceneViewLabel = new GUIStyle(EditorStyles.whiteBoldLabel);
            GiantSceneViewLabel.fontSize = 48;
        }
    }
}
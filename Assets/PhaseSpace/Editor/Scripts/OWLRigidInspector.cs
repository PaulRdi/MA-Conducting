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
    [CustomEditor(typeof(OWLRigidbody))]
    public class OWLRigidInspector : Editor
    {


        //public OWLRigidData rigidData;
        //public int id;
        //public TrackingCondition minCondition = TrackingCondition.Bad;
        //public OWLClient owl;
        //public Space space = Space.Self;

        OWLRigidbody rb;

        SerializedProperty rigidData, id, minCondition, owl, space;

        private void OnEnable()
        {
            rigidData = serializedObject.FindProperty("rigidData");
            id = serializedObject.FindProperty("id");
            minCondition = serializedObject.FindProperty("minCondition");
            owl = serializedObject.FindProperty("owl");
            space = serializedObject.FindProperty("space");
        }

        public override void OnInspectorGUI()
        {

            EditorGUILayout.PropertyField(rigidData);

            if (rigidData.objectReferenceValue != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Id", (int)(rigidData.objectReferenceValue as OWLRigidData).trackerId);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.PropertyField(id);
            }




            EditorGUILayout.PropertyField(owl);
            EditorGUILayout.PropertyField(minCondition);
            EditorGUILayout.PropertyField(space);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
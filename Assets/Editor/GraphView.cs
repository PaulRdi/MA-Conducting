using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

public class GraphView : VisualElement
{


    public new class UxmlFactory : UxmlFactory<GraphView, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

        }



        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }
    }


     
}


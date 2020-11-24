using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;


public class GraphWindow : EditorWindow
{


    [MenuItem("Window/GraphWindow")]
    public static void ShowExample()
    {
        GraphWindow wnd = GetWindow<GraphWindow>();
        wnd.titleContent = new GUIContent("Particle Filter Graph");
    }
    VisualElement rootElement;
    GraphView graphView;
    Image graphImage;
    Texture2D texture;
    int lastDraw;
    bool initialized;
    AccuracyTester tester;
    private void OnEnable()
    {
        int w = 400;
        int h = 200;
        initialized = false;
        rootElement = rootVisualElement;
        VisualTreeAsset vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GraphVisualizer.uxml");
        VisualElement elem = vta.CloneTree();
        rootElement.Add(elem);
        graphImage = rootElement.Query<Image>();

        graphImage.image = texture;

        IMGUIContainer container = rootElement.Query<IMGUIContainer>();
        container.onGUIHandler += HandleDraw;
        //graphView = rootElement.Query<GraphView>();

        //graphView.generateVisualContent += OnContent;
        //graphView.RegisterCallback<GeometryChangedEvent>(OnChangeGeometry);


    }
    private void OnDisable()
    {
        initialized = false;
    }
    private void Update()
    {
        Repaint();
    }
    private void OnInspectorUpdate()
    {
        if (!EditorApplication.isPlaying)
            return;

        if (!initialized)
        {
            InitializeForRecording();
        }
    }

    private void InitializeForRecording()
    {
        initialized = true;
        tester = FindObjectOfType<AccuracyTester>();
    }

    private void HandleDraw()
    {
        if (!initialized)
            return;
        float min = 0.0f;
        float max = 8192.0f;
        float scale = 100.0f;
        float xOffset = 50;
        List<int> refToStuff = tester.recordedValues;
        Vector3[] points = new Vector3[refToStuff.Count];
        EditorGUI.LabelField(new Rect(new Vector2(10.0f, scale), new Vector2(30.0f, 30.0f)), min.ToString());
        EditorGUI.LabelField(new Rect(new Vector2(10.0f, 0.0f), new Vector2(30.0f, 30.0f)), max.ToString());
        for (int i = 0; i < points.Length; i++)
        {
            float t = (float)refToStuff[i] / max;
            points[i] = new Vector3((float)i + xOffset, Mathf.Lerp(scale, 0, t));
        }
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(points);
        Handles.color = Color.black;
        Handles.DrawLine(new Vector3(40.0f, 0.0f), new Vector3(40.0f, scale + 10.0f));
        Handles.DrawLine(new Vector3(30.0f, 0.0f), new Vector3(1050.0f, 0.0f));
        Handles.DrawLine(new Vector3(30.0f, scale), new Vector3(1050.0f, scale));
    }

    private void OnContent(MeshGenerationContext obj)
    {

    }

    private void OnChangeGeometry(GeometryChangedEvent evt)
    {
    }

}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
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
    TestManagerVersion2 testManager;
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
        testManager = FindObjectOfType<TestManagerVersion2>();
    }

    private void HandleDraw()
    {
        if (!initialized)
            return;
        float min = 0.0f;
        float max = 8192.0f;
        float scale = 100.0f;
        float xOffset = 50;
        List<int> particleDeaths = tester.recordedValues;
        List<double> particleDeathsAverages = tester.recordedAverages;
        List<double> accuracies = testManager.recordedAccuracies;
        Vector3[] points = new Vector3[TestConfig.current.maxRecordedValues];
        Vector3[] points2 = new Vector3[TestConfig.current.maxRecordedValues];
        Vector3[] points3 = new Vector3[TestConfig.current.maxRecordedValues];
        Rect graphs1 = EditorGUILayout.BeginVertical();
        EditorGUI.LabelField(new Rect(new Vector2(10.0f, scale), new Vector2(30.0f, 30.0f)), min.ToString());
        EditorGUI.LabelField(new Rect(new Vector2(10.0f, 0.0f), new Vector2(30.0f, 30.0f)), max.ToString());
        

        for (int i = 0; i < TestConfig.current.maxRecordedValues; i++)
        {
            if (i < particleDeaths.Count)
            {
                float t = (float)particleDeaths[i] / max;
                points[i] = new Vector3((float)i + xOffset, Mathf.Lerp(scale, 0, t));
            }

            if (i < particleDeathsAverages.Count)
            {
                float t2 = (float)particleDeathsAverages[i] / max;
                points2[i] = new Vector3((float)i + xOffset, Mathf.Lerp(scale, 0, t2));
            }

            if (i < accuracies.Count)
                points3[i] = new Vector3((float)i + xOffset, Mathf.Lerp(scale, 0, (float)accuracies[i]));
        }

        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(TestConfig.current.graphLineWidth, particleDeaths.Count, points);
        Handles.color = Color.red;
        Handles.DrawAAPolyLine(TestConfig.current.graphLineWidth, particleDeathsAverages.Count, points2);
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(TestConfig.current.graphLineWidth, accuracies.Count, points3);
        Handles.color = Color.black;
        Handles.DrawLine(new Vector3(40.0f, 0.0f), new Vector3(40.0f, scale + 10.0f));
        Handles.DrawLine(new Vector3(30.0f, 0.0f), new Vector3(1050.0f, 0.0f));
        Handles.DrawLine(new Vector3(30.0f, scale), new Vector3(1050.0f, scale));
        EditorGUILayout.EndVertical();
    }

    private void OnContent(MeshGenerationContext obj)
    {

    }

    private void OnChangeGeometry(GeometryChangedEvent evt)
    {
    }

}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Audio;
using UnityEditor;
using System;
using System.Linq;
public enum AudioEditMode
{
    Idle,
    Playing,
    Recording,
    Editing
}

public class AudioEditor : EditorWindow
{
    Song song;
    AudioSource audioSource;
    double dspStart;
    int currBeat;
    int currBeatIndex;
    Texture2D tex;
    int width = 1000;
    int height = 200;
    float[] samplesToVisualize;
    float timePerTexture = 10.0f;
    AudioEditMode state;
    private void Update()
    {
        Repaint();        
    }
    void OnDrawGizmos()
    {
        HandleInput();
    }
    [MenuItem("Window/Audio Editor")]
    public static void CreateAudioPlayer()
    {
        AudioEditor audioEditor = EditorWindow.GetWindow<AudioEditor>();
        audioEditor.Show();
        audioEditor.audioSource = FindObjectOfType<AudioSource>();
        audioEditor.state = AudioEditMode.Idle;
    }
    private void OnGUI()
    {
        //HandleInput();

        GUILayout.BeginHorizontal();
        song = (Song)EditorGUILayout.ObjectField(song, typeof(Song));
        if (GUILayout.Button("Play"))
        {
            TryPlaySong();
        }
        if (GUILayout.Button("Stop"))
        {
            TryStopSong();
        }
        if (GUILayout.Button("Record"))
        {
            TryStartRecord();
        }
        if (GUILayout.Button("Edit"))
        {
            TryStartEdit();
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        Rect rect = new Rect();
        rect.Set(10, 40, 50, 50);

        if (state == AudioEditMode.Recording)
        {
            EditorGUI.DrawRect(rect, Color.red);
        }
        else if (state == AudioEditMode.Playing)
        {
            EditorGUI.DrawRect(rect, Color.green);
        }
        else if (state == AudioEditMode.Editing)
        {
            EditorGUI.DrawRect(rect, Color.yellow);
        }
        else
        {
            EditorGUI.DrawRect(rect, Color.black);

        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (state == AudioEditMode.Playing)
        {

            double currDSP = AudioSettings.dspTime - dspStart;
            if (currBeat < song.beats.Count &&
               currDSP > song.beats[currBeat].dspTime)
            {
                currBeat++;
            }
            if (currBeat < song.beats.Count)
            {
                GUILayout.Label(song.beats[currBeat].beat.ToString());
            }
            GUILayout.EndHorizontal();
            DrawWaveformTextureAtDSPTime(currDSP);
        }
        else if (state == AudioEditMode.Playing ||
                 state == AudioEditMode.Recording)
        {
            GUILayout.Label(currBeat.ToString());
            GUILayout.EndHorizontal();
        }
        else if (state == AudioEditMode.Editing)
        {
            GUILayout.EndHorizontal();
            DrawWaveformTextureAtDSPTime(10.0);
        }
    }

    private void DrawWaveformTextureAtDSPTime(double dspTime)
    {
        GUILayout.BeginHorizontal();
        EditorGUI.DrawPreviewTexture(new Rect(0, 110, width, height),
            Util.PaintWaveformSpectrum(
                samplesToVisualize,
                timePerTexture,
                tex,
                song,
                width, 
                height, 
                Color.grey, 
                dspTime,
                TestConfig.current.beatBuffer));
        GUILayout.EndHorizontal();
    }

    private void TryStartEdit()
    {
        TryStopSong();
        if (song != null)
        {
            state = AudioEditMode.Editing;
            CreateSamplesAndTexture();
        }
    }

    private void HandleInput()
    {
        var e = Event.current;
        if (state == AudioEditMode.Recording &&
                    e != null &&
                    (e.type == EventType.KeyDown &&
                    e.keyCode == KeyCode.G) ||
                    (e.type == EventType.MouseDown &&
                    e.button == 0))
        {
            double currDSP = AudioSettings.dspTime - dspStart;
            song.beats.Add(new Beat(currBeat, currDSP));
            currBeat = (currBeat + 1) % 4;

        }
    }

    private void TryStartRecord()
    {
        TryStopSong();
        currBeat = 0;
        TryPlaySong();
        song.beats = new List<Beat>();
        state = AudioEditMode.Recording;
    }

    public void TryStopSong()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        if (state == AudioEditMode.Recording)
        {
            Undo.RecordObject(song, "Added beats to song");
            EditorUtility.SetDirty(song);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        if (tex != null)
            DestroyImmediate(tex);
        state = AudioEditMode.Idle;
    }
    private void TryPlaySong()
    {
        TryStopSong();
        if (song != null)
        {
            dspStart = AudioSettings.dspTime;
            if (audioSource == null)
            {
                audioSource = new GameObject().AddComponent<AudioSource>();
                audioSource.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            audioSource.Stop();
            audioSource.PlayOneShot(song.audioClip);
            currBeatIndex = 0;
            currBeat = 0;
            state = AudioEditMode.Playing;
            CreateSamplesAndTexture();

        }
    }

    private void CreateSamplesAndTexture()
    {
        samplesToVisualize = Util.GetTextureSamples(this.timePerTexture, song.audioClip, 1000);
        tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
    }

    

   
}

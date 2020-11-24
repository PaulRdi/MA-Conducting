using JetBrains.Annotations;
using PhaseSpace.OWL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public enum MarkerGroupTypes
{
    HAND_LEFT,
    HAND_RIGHT,
    HIPS
}

[JsonObject(Description = "Motion capture data for song")]
public class MoCapData
{
    [JsonProperty]
    public Dictionary<int, MoCapFrame> dspTimeToMoCapFrame;
    [JsonProperty]
    [JsonConverter(typeof(Vector3Converter))]
    Vector3 rootPosition;
    [JsonProperty]
    [JsonConverter(typeof(QuaternionConverter))]
    Quaternion rootRotation;
    [JsonProperty]
    public int numMarkerGroups { get; private set; }
    int currentRecordFrameIndex;
    [JsonProperty]
    public double firstBeatRelativeDSPTime;
    public void SetFirstBeatRelativeDSPTime(double time)
    {
        this.firstBeatRelativeDSPTime = time;
    }

    public MoCapData(Vector3 rootPosition, Quaternion rootRotation)
    {
        dspTimeToMoCapFrame = new Dictionary<int, MoCapFrame>();
        this.rootPosition = rootPosition;
        this.rootRotation = rootRotation;
        this.currentRecordFrameIndex = 0;
        string[] markerGroups = Enum.GetNames(typeof(MarkerGroupTypes));
        this.numMarkerGroups = markerGroups.Length;
    }
    [JsonConstructor]
    public MoCapData(
        Dictionary<int, MoCapFrame> dspTimeToMoCapFrame,
        Vector3 rootPosition,
        Quaternion rootRotation,
        int numMarkerGroups,
        double firstBeatRelativeDSPTime)
    {
        this.dspTimeToMoCapFrame = dspTimeToMoCapFrame;
        this.rootPosition = rootPosition;
        this.rootRotation = rootRotation;
        this.numMarkerGroups = numMarkerGroups;
        this.firstBeatRelativeDSPTime = firstBeatRelativeDSPTime;
    }
    public void RecordFrame(MarkerGroup[] markerGroups, double relativeDspTime)
    {
        if (markerGroups.Length != numMarkerGroups)
            throw new System.Exception("Each frame must be recorded with the amount of marker groups the mocap data was initialized with. \n" +
                "Had " + markerGroups.Length + " marker groups, but initialized with " + numMarkerGroups);


        MoCapFrame frame = new MoCapFrame(markerGroups.Length, relativeDspTime);
        for (int i=0; i < markerGroups.Length; i++)
        {
            frame[i] = markerGroups[i].lastAveragePosition - rootPosition;
        }
        dspTimeToMoCapFrame.Add(currentRecordFrameIndex, frame);
        currentRecordFrameIndex++;
    }

    public void Save(string fileName)
    {
        string json = JsonConvert.SerializeObject(this);
        string fullPath = UnityEngine.Application.streamingAssetsPath + "/" + fileName;
        File.WriteAllText(fullPath, json);
    }
}
[JsonObject]
public class MoCapFrame
{
    [JsonProperty(ItemConverterType = typeof(Vector3Converter))]
    Vector3[] positions;
    [JsonProperty]
    public double relativeDspTime { get; private set; }

    public MoCapFrame(int numMarkerGroupPositions, double relativeDspTime)
    {
        positions = new Vector3[numMarkerGroupPositions];
        this.relativeDspTime = relativeDspTime;
    }

    [JsonConstructor]
    public MoCapFrame(Vector3[] positions, double relativeDspTime)
    {
        this.positions = positions;
        this.relativeDspTime = relativeDspTime;
    }
    [JsonIgnore]
    public Vector3 this[int index]
    {
        get
        {
            return positions[index];
        }
        set
        {
            positions[index] = value;
        }
    }
}

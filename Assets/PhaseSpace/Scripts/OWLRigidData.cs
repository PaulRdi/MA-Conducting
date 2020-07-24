using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniJSON;
using System.IO;

[CreateAssetMenu(fileName = "Rigid", menuName = "PhaseSpace/OWL Rigid Data", order = 1)]
public class OWLRigidData : ScriptableObject
{

    //tracker name
    public string trackerName;

    //tracker id - must be unique
    public uint trackerId = 1;

    //marker ids
    public uint[] ids = new uint[0];

    //marker names
    public string[] names = new string[0];

    //stored in real world millimeters, inverted Z
    public Vector3[] points = new Vector3[0];

    //TODO:  turn this into ninja handle options
    //generic rb options string
    public string options = "";

    //auxiliary data
    public byte[] other;

    public void ReadJSON(string json)
    {
        Dictionary<string, object> root = (Dictionary<string, object>)Json.Deserialize(json);
        List<object> trackers;
        //deal with old json structure
        object rootSomething = root["trackers"];
        if (rootSomething.GetType() == typeof(List<object>))
            trackers = (List<object>)root["trackers"];
        else
            trackers = new List<object>(new object[] { rootSomething });

        Dictionary<string, object> rb = trackers[0] as Dictionary<string, object>;
        trackerId = System.Convert.ToUInt32(rb["id"]);
        //for checking JSON name against current asset name
        trackerName = (string)rb["name"];

        List<object> markers = (List<object>)rb["markers"];
        ids = new uint[markers.Count];
        names = new string[markers.Count];
        points = new Vector3[markers.Count];

        for (int i = 0; i < markers.Count; i++)
        {
            var marker = markers[i] as Dictionary<string, object>;
            ids[i] = System.Convert.ToUInt32(marker["id"]);
            names[i] = (string)marker["name"];
            points[i] = ParsePositionToken((string)marker["options"]);
        }

        //TODO:  Blow away options?  maybe
        options = (string)rb["options"];
    }

    public void LoadFromFile(string path)
    {
        string txt = File.ReadAllText(path);
        ReadJSON(txt);
    }

    Vector3 ParsePositionToken(string str)
    {
        string[] tokens = str.Split('=');
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "pos")
            {
                string[] arr = tokens[i + 1].Split(',');
                return new Vector3(float.Parse(arr[0]), float.Parse(arr[1]), float.Parse(arr[2]));
            }
        }

        Debug.LogWarning("No Position token found!");
        return Vector3.zero;
    }

    public string GetJSON()
    {
        Hashtable root = new Hashtable();
        Hashtable tracker = new Hashtable();
        ArrayList trackers = new ArrayList();
        ArrayList markers = new ArrayList();

        trackers.Add(tracker);

        root.Add("trackers", trackers);
        tracker.Add("name", name);
        tracker.Add("type", "rigid");
        tracker.Add("id", trackerId);
        tracker.Add("markers", markers);
        tracker.Add("options", options);

        for (int i = 0; i < ids.Length; i++)
        {
            Hashtable marker = new Hashtable();
            marker.Add("id", ids[i]);
            marker.Add("name", ids[i].ToString());
            marker.Add("options", string.Format("pos={0},{1},{2}", points[i].x.ToString("f4"), points[i].y.ToString("f4"), points[i].z.ToString("f4")));
            markers.Add(marker);
        }

        return Json.Serialize(root);
    }
}

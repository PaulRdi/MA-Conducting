using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using PhaseSpace.Unity;

public class FullDataReader
{

    public Dictionary<double, FullMocapFrame> data;

    public void Read(string data)
    {
        this.data = new Dictionary<double, FullMocapFrame>();
        string[] frames = data.Split('@');
        foreach(string frameData in frames)
        {
            var frame = FullMocapFrame.Deserialize(frameData);
            this.data.Add(frame.dspTime, frame);
        }
    }

}

public class FullMocapFrame
{
    public Dictionary<int, int> idToArrayIndex;
    public Vector3[] positions;
    public TrackingCondition[] conditions;
    public double dspTime { get; private set; }
    public bool isMarked { get; private set; }

    public Vector3 GetPosition(int markerID)
    {
        return positions[idToArrayIndex[markerID]];
    }

    public TrackingCondition GetCondition(int markerID)
    {
        return conditions[idToArrayIndex[markerID]];
    }

    public static FullMocapFrame Deserialize(string content)
    {
        //parse one frame
        FullMocapFrame output = new FullMocapFrame();
        string[] lines = content.Split('\n');
        int min = 1;
        int max = lines.Length - 1;
        output.idToArrayIndex = new Dictionary<int, int>();   
        output.dspTime = double.Parse(lines[0].Split(':')[1]);

        if (lines[lines.Length-1].StartsWith("m"))
        {
            --max;
            output.isMarked = true;
        }
        else
        {
            output.isMarked = false;
        }

        output.positions = new Vector3[max + 1];
        output.conditions = new TrackingCondition[max + 1];

        for (int i = min; i < max; i++)
        {
            //parse one line (data of single marker)
            int adjustedIndex = i - 1;
            string line = lines[i];
            string[] data = line.Split(',');
            Vector3 pos = Vector3.zero;
            TrackingCondition condition = TrackingCondition.Undefined;
            for (int j = 0; j < 5; j++)
            {
                switch (j)
                {
                    case 0:
                        output.idToArrayIndex.Add(int.Parse(data[j]), adjustedIndex);
                        break;
                    case 1:
                        pos.x = float.Parse(data[j]);
                        break;
                    case 2:
                        pos.y = float.Parse(data[j]);
                        break;
                    case 3:
                        pos.z = float.Parse(data[j]);
                        break;
                    case 4:
                        condition = (TrackingCondition)Enum.Parse(typeof(TrackingCondition), data[j]);
                        break;
                }
            }

            output.positions[adjustedIndex] = pos;
            output.conditions[adjustedIndex] = condition;
        }

        return output;
    }

}


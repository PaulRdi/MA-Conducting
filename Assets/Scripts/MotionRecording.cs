using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionRecording.asset")]
public class MotionRecording : ScriptableObject
{
    public string fileName => _fileName;
    [SerializeField] string _fileName;

    public bool initialized
    {
        get; private set;
    }
    public FullDataReader dataReader
    {
        get; private set;
    }
    public double dspTimeOffset { get; private set; }
    [NonSerialized]
    public double[] dspTimes;
    [NonSerialized]
    public int currentFrameIndex;
    private void Awake()
    {
        initialized = false;
    }
    public void Init(FullDataReader fdr)
    {
        initialized = true;
        currentFrameIndex = 0;
        this.dataReader = fdr;
        this.dspTimes = dataReader.data.Keys.OrderBy(d => d).ToArray();

        var markedFrame = dataReader.data.Values.FirstOrDefault(f => f.isMarked);
        if (markedFrame != default)
        {
            dspTimeOffset = markedFrame.dspTime;
        }
    }

}


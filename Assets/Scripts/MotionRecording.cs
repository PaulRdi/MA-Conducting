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

    private void Awake()
    {
        initialized = false;
    }
    public void Init(FullDataReader fdr)
    {
        initialized = true;
        this.dataReader = fdr;
    }
}


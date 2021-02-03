using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerCustom : MonoBehaviour
{
    public int id;
    public DataSource source;
    public MotionRecording recording;



    void Update()
    {
        transform.position = DataRouter.MPos(source, id, recording);
    }
}

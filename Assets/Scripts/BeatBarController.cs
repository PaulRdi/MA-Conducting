using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
public class BeatBarController : MonoBehaviour
{
    [SerializeField] RectTransform one, two, three, four;
    [SerializeField] AnimationCurve beatProgression;
    [SerializeField] RectTransform circle;
     RectTransform[] beats;

    public int currBeat;
    public double timeToNextBeat;
    public double timeOnCurrentBeat;
    private void Update()
    {
        float animProg = beatProgression.Evaluate((float)(timeOnCurrentBeat / timeToNextBeat));
        circle.position = Vector3.Lerp(
            beats[currBeat].position,
            beats[(currBeat + 1) % beats.Length].position,
            animProg);
    }

    private void Start()
    {
        beats = new RectTransform[]
        {
            one,
            two,
            three,
            four
        };
        timeToNextBeat = 1.0f;

    }


}

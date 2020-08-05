using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using TMPro;
public class BeatBarController : MonoBehaviour
{
    [SerializeField] RectTransform one, two, three, four;
    [SerializeField] AnimationCurve beatProgression;
    [SerializeField] RectTransform circle;
    [SerializeField] TMP_Text beatText, dspText, relativeDspText;
     RectTransform[] beats;

    public int currBeat;
    public double timeToNextBeat;
    public double timeOnCurrentBeat;
    public double startDspTime;
    
    private void Update()
    {
        float animProg = beatProgression.Evaluate((float)(timeOnCurrentBeat / timeToNextBeat));
        circle.position = Vector3.Lerp(
            beats[currBeat].position,
            beats[(currBeat + 1) % beats.Length].position,
            animProg);

        beatText.text = currBeat.ToString();
        dspText.text = Math.Round(AudioSettings.dspTime, 2).ToString();
        relativeDspText.text = Math.Round((AudioSettings.dspTime - startDspTime), 2).ToString();
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

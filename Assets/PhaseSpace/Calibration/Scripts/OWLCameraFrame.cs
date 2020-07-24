/*
* Copyright 2017, PhaseSpace Inc.
* 
* Material contained in this software may not be copied, reproduced to any electronic medium or 
* machine readable form or otherwise duplicated and the information herein may not be used, 
* disseminated or otherwise disclosed, except with the prior written consent of an authorized 
* representative of PhaseSpace Inc.
*
* PhaseSpace and the PhaseSpace logo are registered trademarks, and all PhaseSpace product 
* names are trademarks of PhaseSpace Inc.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PhaseSpace.OWL;

public class OWLCameraFrame : MonoBehaviour
{
    public uint cameraId;
    public Text labelName;
    public GameObject markerPrototype;
    public Transform frameRoot;
    public float frameSize = 512;
    public float sensorSize = 320;
    public RawImage calibrationHistory;
    public int textureSize = 32;
    public Color paintColor;
    public Color[] flagColors;

    RectTransform root;
    Dictionary<uint, RectTransform> markerTable;
    Dictionary<uint, Peak[]> peakTable;
    Texture2D calibratingTexture;

    void Start()
    {
        root = GetComponent<RectTransform>();
        markerTable = new Dictionary<uint, RectTransform>();
        peakTable = new Dictionary<uint, Peak[]>();
        //TODO:  Dynamically set marker count
        PopulateMarkers(20);


        //black and transparent texture
        calibratingTexture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
        calibratingTexture.SetPixels32(new Color32[calibratingTexture.width * calibratingTexture.height]);
        calibratingTexture.filterMode = FilterMode.Point;
        calibratingTexture.wrapMode = TextureWrapMode.Clamp;
        calibratingTexture.Apply();
        calibrationHistory.texture = calibratingTexture;
        calibrationHistory.gameObject.SetActive(true);
    }

    void PopulateMarkers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate<GameObject>(markerPrototype, markerPrototype.transform.parent, true);
            go.SetActive(false);
            markerTable.Add((uint)i, (RectTransform)go.transform);
            peakTable.Add((uint)i, new Peak[2]);
        }
    }

    void Update()
    {
        frameRoot.localScale = Vector3.one * (root.rect.width / frameSize);
    }

    void LateUpdate()
    {

        if (calibrationHistoryUpdated)
        {
            int[][] writes = new int[historyWrites.Count][];
            for (int i = 0; i < historyWrites.Count; i++)
            {
                try
                {
                    writes[i] = historyWrites[i];
                }
                catch
                {
                    //Debug.Log(i);
                }

            }

            historyWrites.Clear();
            calibrationHistoryUpdated = false;

            foreach (var p in writes)
            {
                calibratingTexture.SetPixel(p[0], p[1], paintColor);
            }


            calibratingTexture.Apply();

        }

        //finalize stuff to draw
        foreach (var d in peakTable)
        {
            try
            {
                if (d.Value == null)
                    continue;

                if (d.Value[0] != null && d.Value[1] != null)
                {
                    markerTable[d.Key].gameObject.SetActive(true);
                    markerTable[d.Key].localPosition = new Vector3(d.Value[0].pos * sensorSize, d.Value[1].pos * sensorSize, 0);

                    var image = markerTable[d.Key].GetComponent<Image>();
                    //amplitude low
                    if (((d.Value[0].flags | d.Value[1].flags) & 0x0100) > 0)
                    {
                        image.color = flagColors[0];
                    }
                    //too high
                    else if (((d.Value[0].flags | d.Value[1].flags) & 0x0200) > 0)
                    {
                        image.color = flagColors[2];
                    }
                    else
                    {
                        image.color = flagColors[1];
                    }
                }
                else
                {
                    markerTable[d.Key].gameObject.SetActive(false);
                }
            }
            catch
            {
                //Threading problem? maybe
                //Debug.Log("Whoops: " + d.Key);
                //Debug.Log(d.Value[0]);
                //Debug.Log(d.Value[1]);
            }
        }
    }

    public void ClearPeaks()
    {
        foreach (var d in peakTable)
        {
            d.Value[0] = null;
            d.Value[1] = null;
        }
    }

    public void UpdatePeak(Peak p)
    {
        //no ID
        if (p.id == uint.MaxValue)
            return;

        peakTable[p.id][p.detector] = p;
    }

    public void SetName(string name)
    {
        labelName.text = name;
    }

    bool calibrationHistoryUpdated = false;
    List<int[]> historyWrites = new List<int[]>();

    public void PaintCalibrationHistory(Peak[] peaks)
    {

        int x = Mathf.RoundToInt(peaks[0].pos * textureSize);
        int y = Mathf.RoundToInt(peaks[1].pos * textureSize);

        historyWrites.Add(new int[2] { x, y });

        calibrationHistoryUpdated = true;
    }



}

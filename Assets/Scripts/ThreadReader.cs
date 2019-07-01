using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ViveSR.anipal.Eye;
using static BlinkTracker;

public class ThreadReader : MonoBehaviour
{
    private EyeThreader DataThread = null;
    private EyeData data = new EyeData();

    public Text ShowValues;

    private int[] leftCounter;
    private int[] rightCounter;

    // Use this for initialization
    void Start()
    {
        DataThread = FindObjectOfType<EyeThreader>();
        if (DataThread == null) return;

        leftCounter = new int[3];
        rightCounter = new int[3];
    }

    // You can get data from another thread and use MonoBehaviour's method here.
    // But in Unity's Update function, you can only have 90 FPS.
    void Update()
    {
        if(DataThread.blink_data.Count > 0)
        {
            if(DataThread.blink_data.TryDequeue(out BlinkData bd))
            {
                TreatBlinkData(bd);
            }
        }

        if(ShowValues != null)
            ShowValues.text = "Left Eye Blinks: " + leftCounter[0] + " | " + leftCounter[1] + " | " + leftCounter[2] + "\n" 
                + "Right Eye Blinks: " + rightCounter[0] + " | " + rightCounter[1] + " | " + rightCounter[2];
    }

    
    void TreatBlinkData(BlinkData bd)
    {
        if(bd.eye == EyeID.LEFT)
        {
            // Left
            if(bd.type == BlinkType.SHORT_BLINK)
            {
                leftCounter[0]++;
            }
            else if(bd.type == BlinkType.MEDIUM_BLINK)
            {
                leftCounter[1]++;
            }
            else if (bd.type == BlinkType.EXTENDED_BLINK)
            {
                leftCounter[2]++;
            }
        }
        else
        {
            // Right
            // Left
            if (bd.type == BlinkType.SHORT_BLINK)
            {
                rightCounter[0]++;
            }
            else if (bd.type == BlinkType.MEDIUM_BLINK)
            {
                rightCounter[1]++;
            }
            else if (bd.type == BlinkType.EXTENDED_BLINK)
            {
                rightCounter[2]++;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ViveSR.anipal.Eye;
using static BlinkTracker;
using static DebugLineDrawing;
using static EyeTracker;
using UnityEngine.EventSystems;

public class ThreadReader : MonoBehaviour
{
    private EyeThreader DataThread = null;

    public Text ShowBlinkValues;
    public Image gazeImage;

    public int TARGET_FRAMERATE = 60;

    private int[] leftCounter;
    private int[] rightCounter;

    private Vector3 gaze_value = Vector3.zero;

    // Use this for initialization
    void Start()
    {
        Application.targetFrameRate = TARGET_FRAMERATE;

        DataThread = FindObjectOfType<EyeThreader>();
        if (DataThread == null) return;

        leftCounter = new int[3];
        rightCounter = new int[3];

        EyeParameter ep = new EyeParameter
        {
            gaze_ray_parameter = new GazeRayParameter
            {
                sensitive_factor = 0.1f
            }
        };

        SRanipal_Eye.SetEyeParameter(ep);
    }

    // You can get data from another thread and use MonoBehaviour's method here.
    // But in Unity's Update function, you can only have 90 FPS.
    void Update()
    {
        // Treat Blink Data
        if (DataThread.blink_data.Count > 0)
        {
            if (DataThread.blink_data.TryDequeue(out BlinkData bd))
            {
                TreatBlinkData(bd);
            }
        }

        if (ShowBlinkValues != null)
            ShowBlinkValues.text = "Left Eye Blinks: " + leftCounter[0] + " | " + leftCounter[1] + " | " + leftCounter[2] + "\n" 
                + "Right Eye Blinks: " + rightCounter[0] + " | " + rightCounter[1] + " | " + rightCounter[2];

    }

    private void FixedUpdate()
    {
        // Treat Gaze Data Here
        if (DataThread.gaze_data.Count > 0)
        {
            if (DataThread.gaze_data.TryDequeue(out EyeInfoData gd))
            {
                // Do Stuff
                TreatGazeData(gd);
            }
        }
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


    void TreatGazeData(EyeInfoData gz)
    {
        Vector3 gazeDirection = gz.gaze_ray.direction;

        gazeDirection = Quaternion.Euler(Camera.main.transform.rotation.eulerAngles) * gazeDirection;

        Ray aux = new Ray(Camera.main.transform.position, gazeDirection);
        float dist = Vector3.Distance(aux.origin, gazeImage.canvas.transform.position);

        // This is the point on the actual screen.
        Vector3 distPoint = aux.GetPoint(dist);


        Debug.DrawRay(Camera.main.transform.position, distPoint, Color.red, 20f);
        gazeImage.rectTransform.position = new Vector3(distPoint.x, distPoint.y, distPoint.z);
    }


    void FocusDataTest()
    {
        if(SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out Ray ray))
        {
            Ray aux = new Ray(Camera.main.transform.position, ray.direction);
            RectTransform canvasTransf = gazeImage.canvas.GetComponent<RectTransform>();


            float dist = Vector3.Distance(Camera.main.transform.position, gazeImage.canvas.transform.position);

            Vector3 pt = aux.GetPoint(dist);//gazeImage.canvas.planeDistance);
            

            gazeImage.rectTransform.position = new Vector3(-1 * pt.x, pt.y, pt.z);
        }
    }
}

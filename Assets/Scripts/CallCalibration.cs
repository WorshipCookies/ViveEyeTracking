using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViveSR.anipal.Eye;

public class CallCalibration : MonoBehaviour
{

    private bool calibration_success = false;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        while (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
        {
            // Do Nothing
        }

        calibration_success = false;
    }


    private void Update()
    {
        if (!calibration_success)
        {
            calibration_success = SRanipal_Eye.LaunchEyeCalibration();
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.VR;

public class BaseLineScript : MonoBehaviour
{
    public GameObject m_SteamVRObject;
    public GameObject m_EyeTrackingObject;
    public GameObject m_SRAnipalObject;
    public GameObject m_LSLMarkerObject;
    public GameObject m_TeleportPointerObject;
    public GameObject m_CoinListObject;

    public SteamVR_ActionSet m_JoyStickControls;
    public SteamVR_ActionSet m_TeleportControls;

    public Text m_TimerText;
    public Text m_IntroText;
    public Text m_BaselineText;
    public float m_TotalTimeSeconds = 120f;
    public bool m_IsSetupComplete = false;

    private LSLMarkerScript m_LSLMarkerSystem;
    private bool m_BaselineStart = false;

    // Start is called before the first frame update
    void Awake()
    {
        // Deactivate Controls
        m_JoyStickControls.Deactivate();
        m_TeleportControls.Deactivate();

        m_LSLMarkerSystem = m_LSLMarkerObject.GetComponent<LSLMarkerScript>();

    }

    // Update is called once per frame
    void Update()
    {
        if (m_IsSetupComplete)
        {
            if (!m_BaselineStart)
            {
                m_LSLMarkerSystem.PushData("BASELINE_START");
                m_BaselineStart = true;
            }
            // KickStart Baseline Method
            m_IntroText.enabled = false;
            m_BaselineText.enabled = true;
            m_TimerText.enabled = true;

            m_TimerText.text = "" + (int)m_TotalTimeSeconds + " Seconds";
            m_TotalTimeSeconds -= Time.deltaTime;

            if(m_TotalTimeSeconds <= 0f)
            {
                m_BaselineText.enabled = false;
                m_TimerText.enabled = false;
                m_LSLMarkerSystem.PushData("BASELINE_END");
                

                KickStartExperiment();
            }
        }
    }

    void KickStartExperiment()
    {
        // Make Sure Objects are not Destroyed
        DontDestroyOnLoad(m_SteamVRObject);
        DontDestroyOnLoad(m_EyeTrackingObject);
        DontDestroyOnLoad(m_SRAnipalObject);
        DontDestroyOnLoad(m_LSLMarkerObject);
        DontDestroyOnLoad(m_TeleportPointerObject);
        DontDestroyOnLoad(m_CoinListObject);

        // Activate Controls
        m_JoyStickControls.Activate(SteamVR_Input_Sources.Any, 1, false);
        m_TeleportControls.Activate(SteamVR_Input_Sources.Any, 1, false);

        Camera.main.clearFlags = CameraClearFlags.Skybox;
        Camera.main.backgroundColor = Color.clear;

        m_CoinListObject.SetActive(true);

        m_LSLMarkerSystem.PushData("GAME_START");
        // Load New Scene
        SceneManager.LoadScene(2);
    }
}

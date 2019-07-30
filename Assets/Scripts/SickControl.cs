using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using LSL;

public class SickControl : MonoBehaviour
{
    public SteamVR_Action_Boolean m_SickAction;
    public Text UISick_Text;

    private SteamVR_Behaviour_Pose m_Pose = null;
    private LSLMarkerScript m_LSLSystem;

    public GameObject m_LSLMarkerObject;

    private void Awake()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_LSLSystem = m_LSLMarkerObject.GetComponent<LSLMarkerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_SickAction.GetStateDown(m_Pose.inputSource))
        {
            UISick_Text.enabled = true;
            m_LSLSystem.PushData("SICK_MARKER_DOWN");
            
        }
        if (m_SickAction.GetStateUp(m_Pose.inputSource))
        {
            UISick_Text.enabled = false;
            m_LSLSystem.PushData("SICK_MARKER_UP");
        }
    }
}

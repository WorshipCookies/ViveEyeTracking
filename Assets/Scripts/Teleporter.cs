using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class Teleporter : MonoBehaviour
{
    public bool disableTeleport = false;


    public GameObject m_Pointer;
    public SteamVR_Action_Boolean m_TeleportAction;

    private SteamVR_Behaviour_Pose m_Pose = null;
    private bool m_HasPosition = false;

    private bool m_IsTeleporting = false;
    private float m_FadeTime = 0.5f;

    public Material m_CanTeleport_Material;
    public Material m_CannotTeleport_Material;

    // Start is called before the first frame update
    private void Awake()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Pointer.SetActive(false);
    }

    // Update is called once per frame
    private void Update()
    {

        if (!disableTeleport)
        {
            // Pointer
            m_HasPosition = UpdatePointer();
            //m_Pointer.SetActive(m_HasPosition);

            if (m_HasPosition)
            {
                m_Pointer.GetComponent<MeshRenderer>().material = m_CanTeleport_Material;
            }
            else
            {
                m_Pointer.GetComponent<MeshRenderer>().material = m_CannotTeleport_Material;
            }

            if (m_TeleportAction.GetStateDown(m_Pose.inputSource))
            {
                m_Pointer.SetActive(true);
            }
            // Teleport
            else if (m_TeleportAction.GetStateUp(m_Pose.inputSource))
            {
                TryTeleport();
                m_Pointer.SetActive(false);
            }
        }
    }


    private void TryTeleport()
    {
        // Check for Valid Position and Is the Player Teleporting
        if (!m_HasPosition || m_IsTeleporting)
            return;

        // Get Camera Rig and Head Position
        Transform cameraRig = SteamVR_Render.Top().origin;
        Vector3 headPosition = SteamVR_Render.Top().head.position;

        // Figure out Translation
        Vector3 groundPosition = new Vector3(headPosition.x, cameraRig.position.y, headPosition.z);
        Vector3 translationVector = m_Pointer.transform.position - groundPosition;

        // Move
        StartCoroutine(MoveRig(cameraRig, translationVector));
    }

    private IEnumerator MoveRig(Transform cameraRig, Vector3 translation)
    {
        // Flag - Is Teleporting
        m_IsTeleporting = true;

        // Fade to Black
        SteamVR_Fade.Start(Color.black, m_FadeTime, true);

        // Wait for fade and then apply Transition
        yield return new WaitForSeconds(m_FadeTime);
        cameraRig.position += translation;

        // Fade to Clear
        SteamVR_Fade.Start(Color.clear, m_FadeTime, true);
        yield return new WaitForSeconds(m_FadeTime);

        // Deflag - Is Teleporting
        m_IsTeleporting = false;
    }

    private bool UpdatePointer()
    {
        // Raycast from the controller position
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // If it a hit
        if(Physics.Raycast(ray, out hit))
        {
            // This needs to make sure that we collide with only ground objects
            m_Pointer.transform.position = hit.point;

            if(hit.transform.tag == "Floor" || hit.transform.tag == "Coin")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // If not a hit
        return false;
    }


}

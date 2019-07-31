using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class PlayerMove : MonoBehaviour
{

    public SteamVR_ActionSet m_ActionSet;
    public SteamVR_Action_Vector2 m_MovePostion;
    public SteamVR_Action_Vector2 m_RotatePosition;


    public float m_MaxSpeed = 0.1f;
    public float m_Acceleration = 0.05f;
    public float m_Deceleration = 0.07f;
    private float currentSpeed = 0f;
    private Vector2 lastDirection;

    public float m_TurnSpeedModifier = 5f;
    public float m_MaxTurnSpeed = 2f;
    private float currentRotation;

    private CharacterController characterController;

    public bool disableControllers = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        //m_MovePostion = SteamVR_Actions.StandardControl.Walk;
       
        m_ActionSet.Activate(SteamVR_Input_Sources.Any, 1, false);
        lastDirection = Vector3.zero;
        currentRotation = 0f;
        
    }

    private void Start()
    {
        characterController = this.GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {

        if (!disableControllers)
        {
            // Translation Movement
            Vector2 moveVal = m_MovePostion.GetAxis(SteamVR_Input_Sources.LeftHand);
            TranslationMovement(moveVal);

            Vector2 rotVal = m_RotatePosition.GetAxis(SteamVR_Input_Sources.RightHand);
            RotationMovement(rotVal);
            //Debug.Log("Right Hand " + rotVal);
        }
    }


    void TranslationMovement(Vector2 moveVal)
    {
        Vector3 targetDirection = Vector3.zero;

        // Gradual Acceleration and Deceleration 
        if (moveVal.Equals(Vector2.zero) && currentSpeed != 0f)
        {
            if (currentSpeed > 1f)
            {
                currentSpeed -= (Time.deltaTime * m_Deceleration);
            }
            else
            {
                currentSpeed = 0f;
            }
            targetDirection = Camera.main.transform.TransformDirection(new Vector3(lastDirection.x * currentSpeed, 0f, lastDirection.y * currentSpeed));
        }
        else if (!moveVal.Equals(Vector2.zero))
        {
            if (currentSpeed >= m_MaxSpeed)
            {
                currentSpeed = m_MaxSpeed;
            }
            else
            {
                currentSpeed += Time.deltaTime * m_Acceleration;
            }
            lastDirection = moveVal;
            targetDirection = Camera.main.transform.TransformDirection(new Vector3(moveVal.x * currentSpeed, 0f, moveVal.y * currentSpeed));
        }

        targetDirection.y = 0f;
        characterController.Move(targetDirection);

        //this.transform.position += targetDirection;
    }

    void RotationMovement(Vector2 rotVal)
    {
        if (rotVal.Equals(Vector2.zero))
        {
            currentRotation = 0f;
        }
        else
        {
            currentRotation += m_TurnSpeedModifier * Time.deltaTime;
            if (currentRotation > m_MaxTurnSpeed)
                currentRotation = m_MaxTurnSpeed;

            this.gameObject.transform.Rotate(new Vector3(0f, rotVal.x * currentRotation, 0f));
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltFollowPlayer : MonoBehaviour
{
    [Header("Reference Transforms")]
    [SerializeField] private Transform beltAnchor;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private float minHeight = 0.5f;
    [SerializeField] private PlayerLocomotion locomotion;

    [Header("Sensitivity")]
    [SerializeField] private float minHeadMovement = 0.1f;
    [Tooltip("Minimum head rotation in degrees")]
    [SerializeField] private float minHeadRotation = 30f;
    
    [Header("Speed")]
    [SerializeField] private float rotationDegreesPerSecond = 90f;
    [SerializeField] private float movementSpeed = 1f;

    // Flags define whether the belt is in rotation and/or move mode
    private bool m_NeedsRotation = false;
    private bool m_NeedsMove = false;

    public void LateUpdate()
    {
        if (!m_NeedsRotation)
        {
            CheckIfNeedRotation();
        }


        if (!m_NeedsMove)
        {
            CheckIfNeedMovement();
        }
        if (m_NeedsMove || m_NeedsRotation)
        {
            //Move and rotate together
            MoveTowardsHead();
            RotateTowardsHead();
        }
    }

    // How far the belt needs to rotate around Y to be aligned with the head
    private float RequiredRotation { 
        get {
            Quaternion deltaRotation = Quaternion.Inverse(transform.rotation) * beltAnchor.rotation;
            return Mathf.DeltaAngle(0, deltaRotation.eulerAngles.y);
        }
    }

    // Movement required by the belt to be aligned with the head
    private Vector3 RequiredMovement
    {
        get
        {
            Vector3 currentOffset = transform.position - beltAnchor.position;
            // make sure the belt doesnt go into the floor
            if(beltAnchor.position.y < minHeight)
            {
                currentOffset.y = transform.position.y - minHeight;
            }
            return -currentOffset;
        }
    }

    // Returns true if Y-rotation of the head has changed significantly
    private void CheckIfNeedRotation()
    {
        // Always stay tight to the body
        if (locomotion.IsBodyTurned)
        {
            m_NeedsRotation = true;
        }else if (Mathf.Abs(RequiredRotation) > minHeadRotation)
        {
            m_NeedsRotation = true;
        }
    }

    // Returns true if movement of the body moved or the head has changed significantly
    private void CheckIfNeedMovement()
    {
        
        // Always stay tight to the body
        if (locomotion.IsBodyMoved)
        {
            m_NeedsMove = true;
            return;
        }

        // Check if head movement was significant
        if(RequiredMovement.magnitude > minHeadMovement)
        {
            m_NeedsMove = true;
        }
    }

    private void RotateTowardsHead()
    {
        float rotationDegrees = RequiredRotation;

        // y rotation of camera relative to belt
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(Vector3.up * rotationDegrees);
        float speed = rotationDegrees / 180;
        
        if (locomotion.IsBodyTurned)
        {
            // If we are turning together with the body
            speed = 360f;
        }
        else {
            // If we are gradually turning with the head
            speed = Mathf.Lerp(rotationDegreesPerSecond / 4f, rotationDegreesPerSecond *4f, Mathf.Abs(speed));
            speed *= Time.deltaTime;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed);

        if(Mathf.Abs(rotationDegrees) < 1f)
        {
            m_NeedsRotation = false;
        }
    }


    private void MoveTowardsHead()
    {
        Vector3 requiredMovement = RequiredMovement;

        float speed = requiredMovement.magnitude;
        if (locomotion.IsBodyMoved)
        {
            // If we are moving together with the body
            speed = 100f;
        }
        else
        {
            // If we are moving gradually with the head
            speed = Mathf.Lerp(movementSpeed / 4f, movementSpeed * 4f, Mathf.Abs(speed));
            speed *= Time.deltaTime;
        }

        Vector3 targetPosition = transform.position + requiredMovement;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed);

        if(requiredMovement.magnitude < 0.01f)
        {
            m_NeedsMove = false;
        }
    }
   

}

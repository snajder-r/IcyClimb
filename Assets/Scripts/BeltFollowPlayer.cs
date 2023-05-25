using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltFollowPlayer : MonoBehaviour
{
    [Header("Reference Transforms")]
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Sensitivity")]
    [SerializeField] private float minHeadMovement = 0.1f;
    [Tooltip("Minimum head rotation in degrees")]
    [SerializeField] private float minHeadRotation = 30f;

    [Header("Speed")]
    [SerializeField] private float rotationDegreesPerSecond = 90f;
    [SerializeField] private float movementSpeed = 1f;

    // Standard offset of the belt from the camera
    private Vector3 m_BeltOffset;

    // Flags define whether the belt is in rotation and/or move mode
    private bool m_NeedsRotation = false;
    private bool m_NeedsMove = false;

    public void Start()
    {
        m_BeltOffset = transform.position - mainCamera.position;
    }

    public void Update()
    {
        if (!m_NeedsRotation)
        {
            CheckIfNeedRotation();
        }
        if (m_NeedsRotation)
        {
            RotateTowardsHead();
        }

        if (!m_NeedsMove)
        {
            CheckIfNeedMovement();
        }
        if (m_NeedsMove)
        {
            MoveTowardsHead();
        }
    }

    // How far the belt needs to rotate around Y to be aligned with the head
    private float RequiredRotation { 
        get {
            Quaternion deltaRotation = Quaternion.Inverse(transform.localRotation) * mainCamera.localRotation;
            return Mathf.DeltaAngle(0, deltaRotation.eulerAngles.y);
        }
    }

    // Movement required by the belt to be aligned with the head
    private Vector3 RequiredMovement
    {
        get
        {
            Vector3 currentOffset = transform.position - mainCamera.transform.position;
            return m_BeltOffset - currentOffset;
        }
    }

    // Returns true if Y-rotation of the head has changed significantly
    private void CheckIfNeedRotation()
    {
        if (Mathf.Abs(RequiredRotation) > minHeadRotation)
        {
            m_NeedsRotation = true;
        }
    }

    // Returns true if movement of the head has changed significantly
    private void CheckIfNeedMovement()
    {
        if(RequiredMovement.magnitude > minHeadMovement)
        {
            m_NeedsMove = true;
        }
    }

    private void RotateTowardsHead()
    {
        float rotationDegrees = RequiredRotation;

        if (Mathf.Abs(rotationDegrees) < 1f)
        {
            m_NeedsRotation = false;
            return;
        }
        // y rotation of camera relative to belt
        Quaternion targetRotation = transform.localRotation * Quaternion.Euler(Vector3.up * rotationDegrees);
        float speed = rotationDegrees / 180;
        // Adapt speed to slow down as it gets closer to the target
        speed = Mathf.Lerp(rotationDegreesPerSecond / 4f, rotationDegreesPerSecond *4f, Mathf.Abs(speed));
        speed *= Time.deltaTime;

        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, speed);
    }


    private void MoveTowardsHead()
    {
        Vector3 requiredMovement = RequiredMovement;

        if (requiredMovement.magnitude < 0.1)
        {
            m_NeedsMove = false;
            return;
        }
        float speed = requiredMovement.magnitude;
        // Adapt speed to slow down as it gets closer to the target
        speed = Mathf.Lerp(movementSpeed / 4f, movementSpeed * 4f, Mathf.Abs(speed));
        speed *= Time.deltaTime;
        Vector3 targetPosition = transform.position + requiredMovement;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed);
    }


}

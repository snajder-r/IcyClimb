using UnityEngine;

/// <summary>
/// The behavior of the climbing belt to follow a specified belt anchor
/// </summary>
public class BeltFollowPlayer : MonoBehaviour
{
    [Header("Reference Location")]
    [Tooltip("The transform the belt will attempt to follow")]
    [SerializeField]
    private Transform _beltAnchor;
    [Tooltip("Global minimum y-location. This is mainly important so that the rope cannot spawn below the ground when the game starts.")]
    [SerializeField]
    private float _minHeight = 0.5f;
    [SerializeField] 
    private PlayerLocomotion _locomotion;

    [Header("Sensitivity")]
    [Tooltip("When the head moves more than this amount, the belt will start to follow")]
    [SerializeField]
    private float _minHeadMovement = 0.1f;
    [Tooltip("When the head turns more than this amount, the belt will start to follow")]
    [SerializeField]
    private float _minHeadRotation = 30f;
    
    [Header("Speed")]
    [SerializeField] 
    private float _rotationDegreesPerSecond = 90f;
    [SerializeField] 
    private float _movementSpeed = 1f;

    // Flags define whether the belt is in rotation and/or move mode
    private bool _isNeedRotation = false;
    private bool _isNeedMove = false;


    /// <summary>
    /// How far the belt needs to rotate around Y to be aligned with the head
    /// </summary>
    private float RequiredRotation
    {
        get
        {
            Quaternion deltaRotation = Quaternion.Inverse(transform.rotation) * _beltAnchor.rotation;
            return Mathf.DeltaAngle(0, deltaRotation.eulerAngles.y);
        }
    }

    /// <summary>
    /// Movement required by the belt to be aligned with the head
    /// </summary>
    private Vector3 RequiredMovement
    {
        get
        {
            Vector3 currentOffset = transform.position - _beltAnchor.position;
            // make sure the belt doesnt go into the floor
            if (_beltAnchor.position.y < _minHeight)
            {
                currentOffset.y = transform.position.y - _minHeight;
            }
            return -currentOffset;
        }
    }

    public void LateUpdate()
    {
        if (!_isNeedRotation)
        {
            CheckIfNeedRotation();
        }


        if (!_isNeedMove)
        {
            CheckIfNeedMovement();
        }
        if (_isNeedMove || _isNeedRotation)
        {
            //Originally I moved only when movement was required, and rotated only when rotation was required.
            //Testing, however, revealed that it is more comfortable when movement and rotation go together
            MoveTowardsHead();
            RotateTowardsHead();
        }
    }

    /// <summary>
    /// Sets the _isNeedRotation field to true if rotation is required. 
    /// Does not set it to false otherwise.
    /// </summary>
    private void CheckIfNeedRotation()
    {
        if (_locomotion.IsBodyTurned)
        {
            // Locomotion indicates the actually body has moved, so let's stay tight to the body
            _isNeedRotation = true;
        }else if (Mathf.Abs(RequiredRotation) > _minHeadRotation)
        {
            // The body hasn't moved, but the head has moved by a significant amount
            _isNeedRotation = true;
        }
    }

    // Returns true if movement of the body moved or the head has changed significantly
    private void CheckIfNeedMovement()
    {
        
        // Always stay tight to the body
        if (_locomotion.IsBodyMoved)
        {
            _isNeedMove = true;
            return;
        }

        // Check if head movement was significant
        if(RequiredMovement.magnitude > _minHeadMovement)
        {
            _isNeedMove = true;
        }
    }

    /// <summary>
    /// Rotate the belt around the Y axis to look in the same direction as the belt anchor
    /// </summary>
    private void RotateTowardsHead()
    {
        float rotationDegrees = RequiredRotation;

        // y rotation of camera relative to belt
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(Vector3.up * rotationDegrees);
        float speed = rotationDegrees / 180;
        
        if (_locomotion.IsBodyTurned)
        {
            // If we are turning together with the body it should be instant
            speed = 360f;
        }
        else {
            // If we are merely following the head, it should feel as if the body gradually moved to where the head looks
            speed = Mathf.Lerp(_rotationDegreesPerSecond / 4f, _rotationDegreesPerSecond * 4f, Mathf.Abs(speed));
            speed *= Time.deltaTime;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed);

        if(Mathf.Abs(rotationDegrees) < 1f)
        {
            // Stop rotation if we are close enough
            _isNeedRotation = false;
        }
    }

    /// <summary>
    /// Move the belt to follow the belt anchor
    /// </summary>
    private void MoveTowardsHead()
    {
        Vector3 requiredMovement = RequiredMovement;

        float speed = requiredMovement.magnitude;
        if (_locomotion.IsBodyMoved)
        {
            // If we are moving together with the body
            speed = 100f;
        }
        else
        {
            // If we are moving gradually with the head
            speed = Mathf.Lerp(_movementSpeed / 4f, _movementSpeed * 4f, Mathf.Abs(speed));
            speed *= Time.deltaTime;
        }

        Vector3 targetPosition = transform.position + requiredMovement;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed);

        if(requiredMovement.magnitude < 0.01f)
        {
            // Stop moving if we are close enough
            _isNeedMove = false;
        }
    }
}

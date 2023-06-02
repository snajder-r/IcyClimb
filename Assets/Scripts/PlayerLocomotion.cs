using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerLocomotion : LocomotionProvider
{
    // SERIALIZED PRIVATES FOR INSPECTOR
    [SerializeField]
    private CharacterController characterController;
    [SerializeField]
    private Transform playerCenterOfGravity;

    [Header("Locomotion")]
    [SerializeField]
    private InputActionProperty inputMoveAction;
    [SerializeField]
    private InputActionProperty inputTurnAction;

    [SerializeField]
    private Transform forwardTransform;

    [SerializeField]
    private float walkSpeed = 1f;
    [SerializeField]
    private float turnSpeed = 30f;

    [SerializeField]
    private float inertia = 0.1f;

    [Header("Falling")]
    [SerializeField]
    private SlopeScanner slope;

    [SerializeField]
    private float gravity;

    [SerializeField]
    private float dampen = 0.002f;

    [SerializeField]
    private UnityEvent onFallingEnter;
    [SerializeField]
    private UnityEvent onFalling;
    [SerializeField]
    private UnityEvent onFallingExit;

    [Header("Pulling Locomotion")]
    [SerializeField]
    public List<Hand> hands;

    [SerializeField]
    public float ropeTighteningSpeed = 1f;

    [SerializeField]
    public Transform ropeAnchor;

    public bool IsGrounded { get; private set; }

    /// <summary>
    /// Informs whether we should treat movement as body movement or head movement. 
    /// Can be used by anything that follows the body
    /// </summary>
    public bool IsBodyMoved { get; private set; }
    public bool IsBodyTurned { get; private set; }

    // PUBLIC PROPERTIES


    // PRIVATE FIELDS

    [SerializeField]
    [ShowOnly]
    private Vector3 velocity;

    [SerializeField]
    [ShowOnly]
    private Vector3 cumulativeGravity;

    private bool wasGrounded;
    private bool wasSecured;

    
    private Vector3 finalRopeTaughtPosition;
    private Vector3 ropeTaughtPosition;

    // Normals of the ground we stand on, or Vector.up if in mid air
    private Vector3 floorNormal;

    // If true, no (or very little) gravity should be applied
    private bool isSecured;
    private bool isRopeTaught;

    // SHORT HANDS
    private XROrigin origin => system.xrOrigin;
    private Transform cameraOffset => origin.CameraFloorOffsetObject.transform;
    private Vector2 moveInput => inputMoveAction.action.ReadValue<Vector2>();
    private Vector2 turnInput => inputTurnAction.action.ReadValue<Vector2>();
    private float distanceToAnchor => (ropeAnchor.position - playerCenterOfGravity.position).magnitude;
    private float distanceToGravity => (GetHeadAdjustedGravitationPoint() - playerCenterOfGravity.position).magnitude;

    [SerializeField] Transform debug;

    // METHODS
    protected override void Awake()
    {
        wasGrounded = true;
        isSecured = false;
        isRopeTaught = false;
        velocity = Vector3.zero;
    }

    
    void Update()
    {
        //Initialization
        velocity = (Mathf.Pow(inertia, Time.deltaTime)) * velocity;
        IsBodyMoved = false;
        IsBodyTurned = false;
        isSecured = false;

        PullRopeTaught();

        // Scan if we are on solid ground
        CheckIfGrounded();

        // Check how much we are being pulled and whether we are secured
        velocity += QueryPullProviders();

        // Turning should always be allowed
        ControllerTurn();

        // Moving by controller input should only be allowed if we are grounded
        if (IsGrounded)
        {
            ControllerMove();
        }

        FireEvents();
        

        if (!IsGrounded && !isSecured)
        {
            Fall();
        }

        if (!isSecured) { 
            // Add a small amount of gravity
            velocity -= Vector3.up * 0.01f * gravity * Time.deltaTime;
        }

        Move();

        wasGrounded = IsGrounded;
        wasSecured = isSecured;
    }

    void CheckIfGrounded()
    {
        bool isFlatGround = slope.ScanFallRays(out floorNormal);
        IsGrounded = isFlatGround;
    }
    void FireEvents()
    {
        if(!IsGrounded && !isSecured)
        {
            if(wasGrounded || wasSecured)
            {
                onFallingEnter.Invoke();
            }
            else
            {
                onFalling.Invoke();
            }
        }else if(!wasGrounded && !wasSecured)
        {
            onFallingExit.Invoke();
        }
    }

    private Vector3 GetHeadAdjustedGravitationPoint() {
        Vector3 headOffset = Quaternion.Inverse(cameraOffset.rotation) * origin.Camera.transform.localPosition;
        // Standing tall shouldn't pull you up
        headOffset.y = 0f;
        return ropeTaughtPosition + headOffset;
    }
    void PullRopeTaught()
    {
        finalRopeTaughtPosition = ropeAnchor.position - Vector3.up * distanceToAnchor;
        ropeTaughtPosition += (finalRopeTaughtPosition - ropeTaughtPosition) * ropeTighteningSpeed * Time.deltaTime;

        TrailRenderer trail = debug.GetComponentInChildren<TrailRenderer>();
        debug.transform.position = ropeTaughtPosition;
        trail.Clear();
        trail.AddPosition(ropeAnchor.position);
        trail.AddPosition(ropeTaughtPosition);
    }

    void Move()
    {
        if (!CanBeginLocomotion()) return;
        if (!BeginLocomotion()) return;
        Vector3 toTravel = velocity * Time.deltaTime;
        Vector3 positionBefore = playerCenterOfGravity.position;
        characterController.Move(toTravel);
        Vector3 positionAfter = playerCenterOfGravity.position;

        Vector3 travelled = positionAfter - positionBefore;
        // Dampen gravity in case we collide with something
        cumulativeGravity *= travelled.magnitude / toTravel.magnitude;

        IsBodyMoved = IsBodyMoved || travelled.magnitude > 0f;
        EndLocomotion();
    }

    void Fall()
    {
        Vector3 gravityIncrement = floorNormal.normalized * gravity;

        if (ropeAnchor)
        {
            if(distanceToAnchor > distanceToGravity) {
                // The rope pulls us towards its maximum stretch point

                gravityIncrement = (GetHeadAdjustedGravitationPoint() - playerCenterOfGravity.position);

                if (!isRopeTaught)
                {
                    // We were just caught in the rope, redirect some of the existing velocity
                    cumulativeGravity = gravityIncrement.normalized * cumulativeGravity.magnitude;
                    // But don't let it bounce back up!
                    cumulativeGravity.y = 0f;
                }

                // We are safely caught in the rope which is now fully stretched
                isRopeTaught = true;
            }
            else
            {
                // We have a rope attached but are free-falling
                isRopeTaught = false;
            }
        }
        else
        {
            // We have no rope anchored to the wall yet
            isRopeTaught = false;
        }

        // First dampen existing energy
        cumulativeGravity = cumulativeGravity * Mathf.Pow((1f - dampen), Time.deltaTime);
        cumulativeGravity += gravityIncrement * Time.deltaTime;
        velocity += cumulativeGravity;
    }

    Vector3 QueryPullProviders()
    {
        Vector3 pull = Vector3.zero;
        //isSecured = false;
        foreach (Hand pully in hands)
        {
            pull += pully.GetPull();
            isSecured = isSecured || pully.IsSecured();
        }
        return pull;
    }

    void ControllerTurn()
    {
        if (!CanBeginLocomotion()) return;
        if (!BeginLocomotion()) return;
        Vector3 euler = Vector3.up * turnInput.x * Time.deltaTime * turnSpeed;
        cameraOffset.rotation *= Quaternion.Euler(euler);
        EndLocomotion();

        // If we had controller input induced turn, treat it as a turn of the body
        IsBodyTurned = IsBodyTurned || euler.magnitude > 0f;
    }

    void ControllerMove()
    {
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        // Rotate with camera view
        direction = forwardTransform.rotation * direction;
        // Project on XY plane while retaining magnitude
        direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * direction.magnitude;
        // Multiply with walk speed and add to velocity
        velocity += direction * walkSpeed;
    }
}

public interface IPullProvider
{
    /// <returns>The pulling force and direction excerted by this provider</returns>
    public abstract Vector3 GetPull();

    /// <returns>Whether this provider currently secures the player from falling</returns>
    public abstract bool IsSecured();

    /// <summary>Deals with running out of stamina. Typically release the pully </summary>
    public abstract void OnOutOfStamina();
}

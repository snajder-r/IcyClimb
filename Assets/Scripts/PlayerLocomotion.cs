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
    private float terminalFallingSpeed;

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
    public Rope rope;

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

    private Vector3 floorNormal;

    private bool wasGrounded;
    private bool wasSecured;
    private bool wasRopeTaut;

    // If true, no (or very little) gravity should be applied
    private bool isSecured;

    // SHORT HANDS
    private XROrigin origin => system.xrOrigin;
    private Transform cameraOffset => origin.CameraFloorOffsetObject.transform;
    private Vector2 moveInput => inputMoveAction.action.ReadValue<Vector2>();
    private Vector2 turnInput => inputTurnAction.action.ReadValue<Vector2>();
    [SerializeField] Transform debug;

    // METHODS
    protected override void Awake()
    {
        wasGrounded = true;
        isSecured = false;
        wasRopeTaut = false;
        velocity = Vector3.zero;
    }

    
    void Update()
    {
        //Initialization
        velocity = (Mathf.Pow(inertia, Time.deltaTime)) * velocity;
        IsBodyMoved = false;
        IsBodyTurned = false;
        isSecured = false;

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

        IsBodyMoved = velocity.magnitude > 0f;

        if (!isSecured && !wasRopeTaut) { 
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

    void Move()
    {
        if (!CanBeginLocomotion()) return;
        if (!BeginLocomotion()) return;
        Vector3 toTravel = velocity * Time.deltaTime;
        Vector3 positionBefore = playerCenterOfGravity.position;

        // Fix any uncaught physics bugs
        toTravel = MathUtils.ReplaceNaN(toTravel);
        toTravel = Vector3.ClampMagnitude(toTravel, terminalFallingSpeed);

        characterController.Move(toTravel);
        Vector3 positionAfter = playerCenterOfGravity.position;

        Vector3 travelled = positionAfter - positionBefore;
        // Dampen gravity in case we collide with something
        if(toTravel.magnitude > 0f) { 
            cumulativeGravity *= travelled.magnitude / toTravel.magnitude;
        }

        EndLocomotion();
    }

    void Fall()
    {
        Vector3 gravityIncrement = floorNormal.normalized;
        bool isRopeTaut = rope.IsRopeTaut();
        if (isRopeTaut) { 
            gravityIncrement = (rope.GetFallTowardsPoint() - playerCenterOfGravity.position);
            if (!wasRopeTaut) { 
                // We were just caught in the rope, redirect some of the existing velocity
                cumulativeGravity = gravityIncrement.normalized * cumulativeGravity.magnitude;
                // But absorb all prior vertical velocity
                cumulativeGravity.y = 0f;
            }
        }
        wasRopeTaut = isRopeTaut;

        // First dampen existing energy
        cumulativeGravity = cumulativeGravity * Mathf.Pow(1f - dampen, Time.deltaTime);
        cumulativeGravity += gravityIncrement * Time.deltaTime;

        // Fix any uncaught physics bugs
        cumulativeGravity = MathUtils.ReplaceNaN(cumulativeGravity);
        cumulativeGravity = Vector3.ClampMagnitude(cumulativeGravity, terminalFallingSpeed);
        
        velocity += cumulativeGravity * gravity;
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

using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This is a custom locomotion provider which implements smooth locomotion, smooth turning, as well as falling 
/// and movement by pulling, such as pulling with your ice axes or with rope
/// </summary>
public class PlayerLocomotion : LocomotionProvider
{
    #region "Fields"
    [Header("Pulling Locomotion")]

    public List<Hand> Hands;

    public Rope Rope;
    public XROrigin Origin => system.xrOrigin;
    public Transform CameraOffset => Origin.CameraFloorOffsetObject.transform;

    [SerializeField]
    CharacterController _characterController;

    [SerializeField]
    Transform _playerCenterOfGravity;

    [Header("Locomotion")]
    [SerializeField]
    InputActionProperty _inputMoveAction;

    [SerializeField]
    InputActionProperty _inputTurnAction;

    [SerializeField, Tooltip("Direction used to represent forward when it comes to moving")]
    Transform _forwardTransform;

    [SerializeField]
    float _walkSpeed = 1f;

    [SerializeField, Tooltip("Turn speed in degrees")]
    float _turnSpeed = 30f;

    [SerializeField]
    float _inertia = 0.1f;

    [Header("Falling")]
    [SerializeField]
    SlopeScanner _slope;

    [SerializeField]
    float _gravity;

    [SerializeField]
    float _terminalFallingSpeed;

    [SerializeField, Tooltip("Inertia used only for falling")]
    float _dampen = 0.002f;

    [SerializeField]
    UnityEvent _onFallingEnter;

    [SerializeField]
    UnityEvent _onFalling;

    [SerializeField]
    UnityEvent _onFallingExit;

    /// <summary>
    /// Current velocity. 
    /// </summary>
    [SerializeField, ShowOnly]
    Vector3 _velocity;

    /// <summary>
    /// The part of the current velocity that is caused by gravity and not by pulling or controller based locomotion
    /// We keep track of this separately, because we want to treat inertia differently between gravity based movement and 
    /// other forms of movement.
    /// </summary>
    [SerializeField, ShowOnly]
    Vector3 _cumulativeGravity;

    /// <summary>
    /// If true, no (or very little) gravity should be applied
    /// </summary>
    bool _isSecured;

    /// <summary>
    /// The floor normal as scanned by the SlopeScanner
    /// </summary>
    Vector3 _floorNormal;

    /// <summary>
    /// These booleans with "was" represent the state of the previous frame update in order to detect change of state
    /// </summary>
    bool _wasGrounded;
    bool _wasSecured;
    bool _wasRopeTaut;

    /// <summary>
    /// Short hands for reading input
    /// </summary>
    Vector2 moveInput => _inputMoveAction.action.ReadValue<Vector2>();
    Vector2 turnInput => _inputTurnAction.action.ReadValue<Vector2>();
    #endregion

    #region "Properties"
    [field: SerializeField]
    public bool IsGrounded { get; set; }

    /// <summary>
    /// Informs whether we should treat movement as body movement or head movement. 
    /// Can be used by anything that follows the body
    /// </summary>
    public bool IsBodyMoved { get; private set; }
    public bool IsBodyTurned { get; private set; }
    #endregion

    #region "Lifecycle"
    protected override void Awake()
    {
        _isSecured = false;
        _velocity = Vector3.zero;
        // makes sure we fall to the ground in the first frame
        _wasRopeTaut = true;
        _wasGrounded = false;
        // Make sure we really "touch grass" when we spawn
        _cumulativeGravity = Vector3.up * -100000f;
    }

    /// <summary>
    /// In the update, we calculate how much we need to move but don't perform the movement yet
    /// </summary>
    void Update()
    {
        IsBodyMoved = false;
        IsBodyTurned = false;
        _isSecured = false;

        // Scan if we are on solid ground
        CheckIfGrounded();

        //Initialization using inertia
        _velocity = (Mathf.Pow(_inertia, Time.deltaTime)) * _velocity;

        // Check how much we are being pulled and whether we are secured
        _velocity += QueryPullProviders();

        // Turning should always be allowed
        ControllerTurn();

        // Moving by controller input should only be allowed if we are grounded
        if (IsGrounded)
        {
            ControllerMove();
        }

        FireEvents();

        if (!IsGrounded && !_isSecured)
        {
            // If we aren't on the floor and we aren't secured (e.g. by holding onto an axe lodged in the ice) we fall
            Fall();
        }

        IsBodyMoved = _velocity.magnitude > 0f;

        if (!_isSecured && IsGrounded)
        {
            // Add a small amount of gravity so we always gravitate towards to floor and don't "fall" off every bump in the ground
            _velocity -= Vector3.up * 10f * _gravity * Time.deltaTime;
        }
    }

    /// <summary>
    /// The actual movement is then performed in the LateUpdate. This keeps movement smooth and prevents jittering
    /// </summary>
    void LateUpdate()
    {
        Move();

        _wasGrounded = IsGrounded;
        _wasSecured = _isSecured;
    }
    #endregion

    #region "Methods"
    /// <summary>
    /// Uses the slope scanner to test if we are on solid ground. 
    /// Sets IsGrounded and _floorNormal
    /// </summary>
    void CheckIfGrounded()
    {
        _slope.ScanFallRays(out _floorNormal);
        bool isFlatGround = _slope.ScanFallRays(out _floorNormal);
        IsGrounded = isFlatGround && _characterController.isGrounded;
    }

    /// <summary>
    /// Inform listeners
    /// </summary>
    void FireEvents()
    {
        if (!IsGrounded && !_isSecured)
        {
            if (_wasGrounded || _wasSecured)
            {
                _onFallingEnter.Invoke();
            }
            else
            {
                _onFalling.Invoke();
            }
        }
        else if (!_wasGrounded && !_wasSecured)
        {
            _onFallingExit.Invoke();
        }
    }

    /// <summary>
    /// Performs the actual movement
    /// </summary>
    void Move()
    {
        if (!CanBeginLocomotion()) return;
        if (!BeginLocomotion()) return;
        Vector3 toTravel = _velocity * Time.deltaTime;
        Vector3 positionBefore = _playerCenterOfGravity.position;

        // Fix any uncaught physics bugs
        toTravel = MathUtils.ReplaceNaN(toTravel);
        toTravel = Vector3.ClampMagnitude(toTravel, _terminalFallingSpeed);

        _characterController.Move(toTravel);
        Vector3 positionAfter = _playerCenterOfGravity.position;

        Vector3 travelled = positionAfter - positionBefore;
        // Dampen gravity in case we collide with something
        if (toTravel.magnitude > 0f)
        {
            _cumulativeGravity *= travelled.magnitude / toTravel.magnitude;
        }

        EndLocomotion();
    }

    /// <summary>
    /// Compute cumulative gravity-based velocity and add it to the main velocity
    /// </summary>
    void Fall()
    {
        // By default, base gravity on the floor normal.
        // This means, that we will be pushed away from a sope, allowing us to slide down
        // Note that if we are mid-air the floor normal will be pointing down, so we just fall straight down
        Vector3 gravityIncrement = _floorNormal;

        // Let's make sure we never fall up but always fall a bit down by clamping the y axis
        gravityIncrement.y = Mathf.Clamp(gravityIncrement.y, -1f, -0.1f);

        // We check if the rope is taut. If it is, we cannot fall further away from the rope,
        // but rather we will fall towards the point which would straighten the rope down without overstretching it
        bool isRopeTaut = Rope.IsRopeTaut();
        if (isRopeTaut)
        {
            gravityIncrement = (Rope.FallTowardsPoint - _playerCenterOfGravity.position);
            if (!_wasRopeTaut)
            {
                // We were just caught in the rope, absorb all prior vertical velocity
                _cumulativeGravity.y = 0f;
            }
        }
        _wasRopeTaut = isRopeTaut;

        // First dampen existing energy
        _cumulativeGravity *= Mathf.Pow(1f - _dampen, Time.deltaTime);
        _cumulativeGravity += gravityIncrement * Time.deltaTime;

        // Fix any uncaught physics bugs
        _cumulativeGravity = MathUtils.ReplaceNaN(_cumulativeGravity);
        _cumulativeGravity = Vector3.ClampMagnitude(_cumulativeGravity, _terminalFallingSpeed);

        _velocity += _cumulativeGravity * _gravity;
    }

    /// <summary>
    /// Check both hands for whether they are pulling is anywhere and add up the pull
    /// Also sets isSecured to true if one of the two hands is securing us.
    /// </summary>
    /// <returns>The total pull from both hands</returns>
    Vector3 QueryPullProviders()
    {
        Vector3 pull = Vector3.zero;
        //isSecured = false;
        foreach (Hand pully in Hands)
        {
            pull += pully.Pull();
            _isSecured = _isSecured || pully.IsSecured;
        }
        return pull;
    }

    /// <summary>
    /// Perform smooth turning based on controller input
    /// </summary>
    void ControllerTurn()
    {
        if (!CanBeginLocomotion()) return;
        if (!BeginLocomotion()) return;
        float turnAngle = turnInput.x * Time.deltaTime * _turnSpeed;
        // It is very important we use this function to rotate ourselves, otherwise 
        // we might orbit around the XR origin which the VR headset might place anywhere in the room
        Origin.RotateAroundCameraUsingOriginUp(turnAngle);
        EndLocomotion();

        // If we had controller input induced turn, treat it as a turn of the body
        IsBodyTurned = IsBodyTurned || !Mathf.Approximately(turnAngle, 0f);
    }

    /// <summary>
    /// Compute the amount of locomotion required and add it to velocity
    /// </summary>
    void ControllerMove()
    {
        Vector3 direction = new(moveInput.x, 0f, moveInput.y);
        // Rotate with camera view
        direction = _forwardTransform.rotation * direction;
        // Project on XY plane while retaining magnitude
        direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * direction.magnitude;
        // Multiply with walk speed and add to velocity
        _velocity += direction * _walkSpeed;
    }
    #endregion
}
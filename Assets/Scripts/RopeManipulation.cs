using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This DropablePully follows the rope such that it will be as close to the player's hand as possible.
/// When the player grabs it, it will insert a chainlink into the rope which then follows the pully.
/// If the player pulls the pully towards themselves, it counts as a "Pull" and is used in locomotion.
/// If the player pulls the pully away from themselves, it can be used to manipulate the rope, such as 
/// attaching it to a wall anchor.
/// </summary>
public class RopeManipulation : DropablePully
{
    [SerializeField, Tooltip("The earlierst point in the rope at which the player is allowed to grab the rope")] 
    private ChainLink _startLink;
    [SerializeField, Tooltip("The latest point in the rope at which the player is allowed to grab the rope")] 
    private ChainLink _endLink;
    [SerializeField, Tooltip("The chainLink that should be right after any wall anchor we might secure the rope in.")] 
    private ChainLink _insertionPointLink;
    [SerializeField, Tooltip("Which hand should be followed. There should be one RopeManipulation component for each hand.")] 
    private XRBaseInteractor _hand;
    [SerializeField] 
    private Rope _rope;
    [SerializeField] 
    private BelayDevice _belayDevice;

    /// <summary>
    /// The closest chainlink that would be the previous link when inserting the manipulator in the chain
    /// </summary>
    private ChainLink _closestPredecessorLink;

    /// <summary>
    /// The current amount of pulling force as computed in the last Update
    /// </summary>
    Vector3 _pull;

    /// <summary>
    /// When the rope is grabbed, this stores the world coordinates where the grab occurred, so we can 
    /// later determine whether we are pulling towards ourselves or away from ourselves
    /// </summary>
    private Vector3 grabCoordinates;

    [Tooltip("The chain link we will insert when we are grabbed")]
    [field: SerializeField] public ChainLink ManipulatorChainLink { get; private set; }

    // Holding a rope does not secure you against gravity
    public override bool IsSecured { get => isSelected && _rope.IsRopeTaut(); }

    private void Start()
    {
        // We only need this when we are activated
        ManipulatorChainLink.GetComponent<Rigidbody>().isKinematic = true;
        ManipulatorChainLink.gameObject.SetActive(false);
    }

    void Update()
    {
        ManipulatorChainLink.transform.position = transform.position;

        _pull = Vector3.zero;
        if (isSelected)
        {
            UpdateGrabbedMovement();
        }
        else { 
            // Move myself along the rope as close to the hand as possible.
            Vector3 closestPoint = GetClosestPointOnRope(out _closestPredecessorLink);
            transform.position = closestPoint;
        }
    }

    public override void OnOutOfStamina()
    {
        ForceRelease(1f);
    }

    public override Vector3 Pull() => _pull;

    void UpdateGrabbedMovement()
    {
        // Direction of rope pull relative to where we grabbed the rope
        Vector3 ropePullDirection = (transform.position - grabCoordinates);

        // Direction in which the pull would pull the player towards the rope, rather then the rope away from the player
        Vector3 toPlayerDirection = PlayerController.Instance.PlayerCenterOfGravity.position - grabCoordinates;

        if(Vector3.Dot(ropePullDirection, toPlayerDirection) < 0)
        {
            // We are pulling the rope away from us
            ManipulatorChainLink.GetComponent<Rigidbody>().MovePosition(transform.position);
        }
        else
        {
            // We are pulling the player towards the rope
            _pull = -ropePullDirection;
        }
    }

    /// <summary>
    /// Follow the rope and find here the closest point to the hand is.
    /// </summary>
    /// <param name="closestLink">Output: The closest chain link</param>
    /// <returns>The closest coordinates</returns>
    Vector3 GetClosestPointOnRope(out ChainLink closestLink)
    {
        ChainLink linkA = _startLink;
        ChainLink linkB;

        Vector3 closestPosition = Vector3.zero;
        closestLink = _startLink;
        float shortestDistance = float.PositiveInfinity;

        Vector3 handPosition = _hand.transform.position;
        // Go through the rope and determine closest point on each rope segment, then check whether it is the globally closest point
        while (true)
        {
            linkB = linkA.NextLink;
            if (linkB is null) break;

            // Get the closest point on this rope segment
            Vector3 candidatePosition = GetClosestPointOnSegment(handPosition, linkA.transform.position, linkB.transform.position);
            float distance = (candidatePosition - handPosition).magnitude;
            // Check if it is the globally closest point (so far)
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestPosition = candidatePosition;
                closestLink = linkA;
            }
            linkA = linkB;
            if (linkB == _endLink) break;
        }
        return closestPosition;
    }

    /// <summary>
    /// Find the closest point on a line to a given point.
    /// Written with help from https://stackoverflow.com/questions/51905268/how-to-find-closest-point-on-line
    /// </summary>
    /// <param name="point">Point to which we are looking for the closest point to</param>
    /// <param name="start">Start point of the line we search</param>
    /// <param name="end">End point of the line we search</param>
    /// <returns></returns>
    Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        direction.Normalize();
        // The dot product as a projection. Since the direction is normalized
        // the outcome is the magnitude of the projection of a onto b.
        float projection_distance = Vector3.Dot(point - start, direction);
        projection_distance = Mathf.Clamp(projection_distance, 0f, length);
        return start + direction * projection_distance;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        grabCoordinates = ManipulatorChainLink.transform.position;
        ManipulatorChainLink.gameObject.SetActive(true);
        // Attach our manipulator link between the two closest links
        _closestPredecessorLink.InsertAfter(ManipulatorChainLink);

        // Tell the rope to not extend while we are pulling ourselves
        _belayDevice.NumberOfManualEngageBreak += 1;
    }

    private void ReconnectLinksAroundManipulator()
    {
        ManipulatorChainLink.DisconnectSelf();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        ManipulatorChainLink.gameObject.SetActive(false);
        // Reconnect the other two chain links
        ReconnectLinksAroundManipulator();
        _belayDevice.NumberOfManualEngageBreak -= 1;
    }

    /// <summary>
    /// Called by a Wall Anchor to let us know they will be taking over the ChainLink
    /// </summary>
    public void OnRopeAttached()
    {
        // Let's lose our grip
        ForceRelease(1f);

        // Hand over ownership
        ManipulatorChainLink.transform.parent = null;
        ReconnectLinksAroundManipulator();

        // We hand over our manipulator chain link to the wall anchor, so we create a new one for ourselves
        // This is also important because we don't want to ruin the chain when OnSelectExited is called
        ManipulatorChainLink = Instantiate(ManipulatorChainLink, transform.position, transform.rotation);
        ManipulatorChainLink.DisconnectSelf(false);
        ManipulatorChainLink.GetComponent<ChainLink>().Start();
        ManipulatorChainLink.transform.parent = transform;
        ManipulatorChainLink.gameObject.SetActive(false);
    }

    /// <summary>
    /// This is so the WallAnchor can ask as where we would like them to connect wall anchors, because we don't want them to be on our manipulateable chain
    /// </summary>
    /// <returns></returns>
    public ChainLink GetChainLinkToPrependNewWallAnchors()
    {
        return _insertionPointLink;
    }
}

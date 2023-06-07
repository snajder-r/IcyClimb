using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RopeManipulation : DropablePully
{
    [SerializeField] private ChainLink startLink;
    [SerializeField] private ChainLink endLink;
    [SerializeField] private ChainLink insertionPointLink;

    [Tooltip("The chain link we will insert when we are grabbed")]
    [field:SerializeField] public ChainLink ManipulatorChainLink { get; private set; }

    [SerializeField] private XRBaseInteractor hand;

    private ChainLink closestPredecessorLink;

    Vector3 pull;
    public override Vector3 GetPull() => pull;

    // Holding a rope does not secure you against gravity
    public override bool IsSecured() => false;

    public override void OnOutOfStamina() {
        ForceRelease(1f);
    }

    /// <summary>
    /// When the rope is grabbed, this stores the world coordinates where the grab occurred
    /// </summary>
    private Vector3 grabCoordinates;

    private void Start()
    {
        // We only need this when we are activated
        ManipulatorChainLink.GetComponent<Rigidbody>().isKinematic = true;
        ManipulatorChainLink.gameObject.SetActive(false);
    }

    void Update()
    {
        ManipulatorChainLink.transform.position = transform.position;

        pull = Vector3.zero;
        if (isSelected)
        {
            UpdateGrabbedMovement();
        }
        else { 
            // Move myself along the rope as close to the hand as possible.
            Vector3 closestPoint = GetClosestPointOnRope(out closestPredecessorLink);
            transform.position = closestPoint;
        }
    }

    void UpdateGrabbedMovement()
    {
        // Direction of rope pull relative to where we grabbed the rope
        Vector3 ropePullDirection = (transform.position - grabCoordinates);

        // Direction in which the pull would pull the player towards the rope, rather then the rope away from the player
        Vector3 toPlayerDirection = PlayerController.instance.playerCenterOfGravity.position - grabCoordinates;

        if(Vector3.Dot(ropePullDirection, toPlayerDirection) < 0)
        {
            // We are pulling the rope away from us
            ManipulatorChainLink.GetComponent<Rigidbody>().MovePosition(transform.position);
        }
        else
        {
            // We are pulling the player towards the rope
            pull = -ropePullDirection;
        }
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return !grabDisabled && base.IsSelectableBy(interactor);
    }

    Vector3 GetClosestPointOnRope(out ChainLink closestLink)
    {
        ChainLink linkA = startLink;
        ChainLink linkB;

        Vector3 closestPosition = Vector3.zero;
        closestLink = startLink;
        float shortestDistance = float.PositiveInfinity;

        Vector3 handPosition = hand.transform.position;
        while (true)
        {
            linkB = linkA.nextLink;
            if (linkB is null) break;

            Vector3 candidatePosition = GetClosestPointOnSegment(handPosition, linkA.transform.position, linkB.transform.position);
            float distance = (candidatePosition - handPosition).magnitude;
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestPosition = candidatePosition;
                closestLink = linkA;
            }
            linkA = linkB;
            if (linkB == endLink) break;
        }
        return closestPosition;
    }

    Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        direction.Normalize();
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
        closestPredecessorLink.InsertAfter(ManipulatorChainLink);
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
        return insertionPointLink;
    }
}

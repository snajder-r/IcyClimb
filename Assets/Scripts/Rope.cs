using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class manages the rope and wall anchors
/// </summary>
public class Rope : MonoBehaviour
{
    public static Rope instance;

    [SerializeField, Tooltip("Whether the rope is currently allowed to stretch in length. " +
        "This is typically controlled by the BelayDevice which decides when the climber is secured in the rope. " +
        "If this is false the player cannot move or fall further from the last secured wall anchor than their current distance.")]
    public bool IsExtendingRope;

    [SerializeField, Tooltip("The first point where the rope is secured at the beginning of the game.")]
    WallAnchor _floorWallAnchor;
    [SerializeField, Tooltip("The chain link of the rope that is closest to the player and before the wall links.")]
    ChainLink _beltRopeAnchor;
    [SerializeField, Tooltip("Which layers are considered wall for the purpose of calculating what blocks the rope from going through it.")]
    LayerMask _wallLayer;
    [SerializeField, Tooltip("How much the rope should be extended before it is taut when it is allowed to extend. Higher values will make the rope less secure, while lower values may lead to jagged movement.")]
    float _ropeExtensionIncrement;

    /// <summary>
    /// Sorted chain of wall anchors in which the rope had been secured
    /// </summary>
    Stack<WallAnchor> _securedWallAnchors;

    [field: SerializeField, Tooltip("How much the rope is currently extending from the last wall anchor")]
    public float RopeExtension { get; set; }

    /// <summary>
    /// The point the player would fall towards before hanging int he rope.
    /// That is, the point below the last wall anchor, at a distance of the current RopeExtension
    /// </summary>
    public Vector3 FallTowardsPoint => _securedWallAnchors.Peek().transform.position - Vector3.up * RopeExtension;

    public Rope() : base()
    {
        instance = this;
    }

    void Start()
    {
        _securedWallAnchors = new Stack<WallAnchor>();
        IsExtendingRope = true;
        WallAnchorSecured(_floorWallAnchor);
    }

    void Update()
    {
        if (IsExtendingRope)
        {
            RopeExtension = ComputeRopeLength() + _ropeExtensionIncrement;
        }
        else
        {
            //If we are not extending, we are still tightening
            RopeExtension = Mathf.Min(RopeExtension, ComputeRopeLength());
        }
    }

    /// <summary>
    /// Whether the rope has extended to its maximum length
    /// </summary>
    public bool IsRopeTaut()
    {
        float distanceToAnchor = ComputeRopeLength(_securedWallAnchors.Peek().transform);
        return distanceToAnchor >= RopeExtension;
    }

    /// <summary>
    /// Add a wall anchor to the stack of secured anchor
    /// </summary>
    public void WallAnchorSecured(WallAnchor anchor)
    {
        _securedWallAnchors.Push(anchor);
    }

    /// <summary>
    /// Remove a wall anchor from the top of the stack of secured anchors, but only if it really is on the top
    /// </summary>
    public void WallAnchorRemoved(WallAnchor anchor)
    {
        if (!IsWallAnchorLastInChain(anchor)) return;
        _securedWallAnchors.Pop();
    }
    /// <summary>
    /// Check if a wall anchor is really the last one (on the top of the stack)
    /// </summary>
    public bool IsWallAnchorLastInChain(WallAnchor anchor)
    {
        return anchor == _securedWallAnchors.Peek();
    }

    float ComputeRopeLength() => ComputeRopeLength(null);

    /// <summary>
    /// Cumulative distance when following the chainlinks from the player back to the last wall anchor.
    /// If takeShortcutTo is provided, we will always check whether we jump straight from any chainlink to
    /// that shortcut position. This is used to avoid situations where the rope physics might have been too slow
    /// to pull the rope taut, but for gameplay reasons we want to assume that the rope is taut.
    /// </summary>
    float ComputeRopeLength(Transform takeShortcutTo)
    {
        float ropeLength = 0f;
        ChainLink link = _beltRopeAnchor;
        ChainLink previousLink = link.PreviousLink;
        // Walk the chainlinks back and compute the total rope length
        while (previousLink)
        {
            if (takeShortcutTo)
            {
                Vector3 from = link.transform.position;
                Vector3 direction = takeShortcutTo.position - from;
                if (!Physics.Raycast(from, direction, Mathf.Infinity, _wallLayer))
                {
                    // We didn't hit any wall on the way to the shortcut
                    // Take the shorcut and end the pathfinding
                    ropeLength += (takeShortcutTo.position - link.transform.position).magnitude;
                    break;
                }
            }

            ropeLength += (previousLink.transform.position - link.transform.position).magnitude;
            link = previousLink;
            previousLink = link.PreviousLink;
            if (link == _securedWallAnchors.Peek()._link)
            {
                // We only search back until the most recently secured wall anchor
                break;
            }
        }
        return ropeLength;
    }
}

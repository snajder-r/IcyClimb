using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public static Rope instance;

    public Rope() : base()
    {
        instance = this;
    }

    [SerializeField] private WallAnchor floorWallAnchor;
    [SerializeField] private ChainLink beltRopeAnchor;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] public bool IsExtendingRope;
    [SerializeField] float ropeExtensionIncrement;
    

    [field:SerializeField]
    public float RopeExtension { get; set; }
    
    private Stack<WallAnchor> securedWallAnchors;

    public bool IsRopeTaut()
    {
        float distanceToAnchor = ComputeRopeLength(securedWallAnchors.Peek().transform);
        return distanceToAnchor >= RopeExtension;
    }

    public Vector3 GetFallTowardsPoint()
    {
        return securedWallAnchors.Peek().transform.position - Vector3.up * RopeExtension;
    }

    void Start()
    {
        securedWallAnchors = new Stack<WallAnchor>();
        IsExtendingRope = true;
        WallAnchorSecured(floorWallAnchor);
    }

    void Update()
    {
        if (IsExtendingRope) {
            RopeExtension = ComputeRopeLength() + ropeExtensionIncrement;
        }
        else
        {
            //If we are not extending, we are still tightening
            RopeExtension = Mathf.Min(RopeExtension, ComputeRopeLength());
        }
    }

    float ComputeRopeLength() => ComputeRopeLength(null);

    float ComputeRopeLength(Transform takeShortcutTo)
    {
        float ropeLength = 0f;
        ChainLink link = beltRopeAnchor;
        ChainLink previousLink = link.previousLink;
        while (previousLink)
        {
            if (takeShortcutTo) {
                Vector3 from = link.transform.position;
                Vector3 direction = takeShortcutTo.position-from;
                if(!Physics.Raycast(from, direction, Mathf.Infinity, wallLayer))
                {
                    // We didn't hit any wall on the way to the shortcut
                    // Take the shorcut and end the pathfinding
                    ropeLength += (takeShortcutTo.position - link.transform.position).magnitude;
                    break;
                }
            }
            
            ropeLength += (previousLink.transform.position - link.transform.position).magnitude;
            link = previousLink;
            previousLink = link.previousLink;
            if (link == securedWallAnchors.Peek().link)
            {
                // We only search back until the most recently secured wall anchor
                break;
            }
        }
        return ropeLength;
    }

    public void WallAnchorSecured(WallAnchor anchor)
    {
        securedWallAnchors.Push(anchor);
    }

    public void WallAnchorRemoved(WallAnchor anchor)
    {
        if (!IsWallAnchorLastInChain(anchor)) return;
        securedWallAnchors.Pop();
    }
    public bool IsWallAnchorLastInChain(WallAnchor anchor)
    {
        return anchor == securedWallAnchors.Peek();
    }


}

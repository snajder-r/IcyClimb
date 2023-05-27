using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WallAnchor : XRGrabInteractable
{
    [SerializeField] Transform ropeAttachPoint;

    private ChainLink link;

    private Rigidbody myRigidBody;

    private bool isAttachedToWall = false;

    void Start()
    {
        myRigidBody = GetComponent<Rigidbody>();
    }


    void Update()
    {
        
    }

    public void OnWallCollisionEnter(Collider cliff)
    {
        myRigidBody.isKinematic = true;
        isAttachedToWall = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        // Only do something if we are in a wall
        if (!isAttachedToWall) return;

        // If we already have a link attached, ignore the trigger
        if (link) return;

        // Get the link which the player is trying to attach to us
        link = other.GetComponent<ChainLink>();
        if (!link) return;

        // Get the manipulator that has been used to move the link towards us.
        RopeManipulation manipulator = link.GetComponentInParent<RopeManipulation>();
        // This also ensures that we don't just catch the rope by chance
        if (!manipulator) return;

        // Inform the rope manipulator that we will take responsibility for this chain link
        manipulator.OnRopeAttached();
        // Take over parenthood
        link.transform.parent = transform;

        ChainLink insertPoint = manipulator.GetChainLinkToPrependNewWallAnchors();
        insertPoint.previousLink.nextLink = link;
        link.OnLinkConnected(insertPoint.previousLink);
        link.nextLink = insertPoint;
        insertPoint.OnLinkConnected(link);
        link.gameObject.SetActive(true);

        
        link.transform.position = ropeAttachPoint.position;

        Rigidbody linkRb = link.GetComponent<Rigidbody>();
        // I think this is true anyways, but let's make sure
        linkRb.isKinematic = true;

        //Tell the player that we are secured
        PlayerController.instance.WallAnchorSecured(this);
    }

    
}

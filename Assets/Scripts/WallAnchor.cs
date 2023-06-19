using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WallAnchor : LodgeAbleGrabbable, IWallTriggerCollider
{
    [SerializeField] Transform ropeAttachPoint;
    [SerializeField] AudioClip lodgeSound;
    [SerializeField] AudioClip dislodgeSound;

    Rope rope;

    public ChainLink link { get; private set; }

    public override Vector3 GetPull() => Vector3.zero;

    public override bool IsSecured() => false;

    private AudioSource audioSource;

    void Start()
    {
        rope = Rope.instance;
        remainsLodgedIfReleased = true;
        audioSource = GetComponent<AudioSource>();
    }

    public void OnWallCollisionEnter(Collider cliff)
    {
        // It can only be lodged by hand
        if (heldController == null) return;
        if (Lodge())
        {
            PlayLodgeSound();
        }
    }

    private void PlayLodgeSound()
    {
        audioSource.PlayOneShot(lodgeSound);
    }

    private void PlayDisLodgeSound()
    {
        audioSource.PlayOneShot(dislodgeSound);
    }

    public override bool Dislodge()
    {
        if(link is not null)
        {
            // We currently have a rope attached
            if (rope.IsWallAnchorLastInChain(this))
            {
                // We are allowed to disconnect the rope
                DisconnectRope();
            }
            else
            {
                // We are not allowed to disconnect the rope and thus can't dislodge
                return false;
            }
        }
        if (base.Dislodge())
        {
            PlayDisLodgeSound();
            return true;
        }
        return false;
    }

    public void DisconnectRope()
    {
        rope.WallAnchorRemoved(this);
        link.DisconnectSelf();
        Destroy(link.gameObject);
        link = null;
    }

    public void OnTriggerEnter(Collider other)
    {
        // Only do something if we are in a wall
        if (!isLodged) return;

        // If we already have a link attached, ignore the trigger
        if (link) return;

        // Get the manipulator that has been used to move the link towards us.
        RopeManipulation manipulator = other.GetComponent<RopeManipulation>();
        // This also ensures that we don't just catch the rope by chance
        if (!manipulator) return;

        // Only a manipulator that is selected (held in hand) should trigger me
        if (!manipulator.isSelected) return;

        // Get the link which the player is trying to attach to us
        link = manipulator.ManipulatorChainLink;
        if (!link) return;

        // Inform the rope manipulator that we will take responsibility for this chain link
        manipulator.OnRopeAttached();
        // Take over parenthood
        link.transform.parent = transform;

        ChainLink insertPoint = manipulator.GetChainLinkToPrependNewWallAnchors();
        insertPoint.InsertBefore(link);
        link.gameObject.SetActive(true);
        
        link.transform.position = ropeAttachPoint.position;

        //Tell the player that we are secured
        rope.WallAnchorSecured(this);
    }

    


}

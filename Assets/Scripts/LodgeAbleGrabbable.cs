using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class LodgeAbleGrabbable : XRGrabInteractable
{
    [SerializeField] protected XRSocketInteractor returnToHolster;
    [SerializeField] protected bool remainsLodgedIfReleased;

    public Rigidbody rigidBody { get; private set; }
    protected XRBaseController heldController;
    protected bool grabDisabled = false;

    /// <summary>
    /// Whether the object is currently lodged into ice.
    /// </summary>
    public bool isLodged { get; private set; }

    public void SendHapticImpulse(float intensity, float duration)
    {
        if (heldController)
        {
            heldController.SendHapticImpulse(intensity, duration);
        }
    }


    protected override void Awake()
    {
        base.Awake();
        rigidBody = GetComponent<Rigidbody>();
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return !grabDisabled && base.IsSelectableBy(interactor);
    }
    protected void ReenableGrab()
    {
        grabDisabled = false;
    }

    protected void ForceRelease()
    {
        grabDisabled = true;

        // One second cooldown before we can grab again
        Invoke("ReenableGrab", 1f);
    }

    protected virtual void GoBackToHolster()
    {
        if (this.isSelected)
        {
            //Don't return to holster if the player re-established the grip
            return;
        }

        if(remainsLodgedIfReleased && isLodged)
        {
            // Don't return to holster if this object is meant to stay lodged
            return;
        }

        Dislodge();
        returnToHolster.StartManualInteraction((IXRSelectInteractable)this);
    }

    public virtual void Lodge()
    {
        // First, freeze the pick in its position.
        rigidBody.constraints = RigidbodyConstraints.FreezeAll;

        // Play haptics
        if (heldController)
        {
            heldController.SendHapticImpulse(1f, 0.25f);
        }

        // Record that it's lodged
        isLodged = true;


    }

    public virtual void Dislodge()
    {
        if (!isLodged)
        {
            return;
        }

        // Unfreeze the pick from its position.
        rigidBody.constraints = RigidbodyConstraints.None;

        // Play haptics
        if (heldController)
        {
            heldController.SendHapticImpulse(0.1f, 0.1f);
        }

        isLodged = false;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        // Store which controller holds it, in order to use haptics
        if (args.interactorObject is XRBaseControllerInteractor)
        {
            heldController = ((XRBaseControllerInteractor)args.interactorObject).xrController;
            heldController.SendHapticImpulse(0.5f, 0.1f);
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (args.interactorObject is XRBaseControllerInteractor)
        {
            // If it was deselected from hand (not from the holster)
            heldController = null;
            Invoke("GoBackToHolster", 3f);
        }
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        Dislodge();
    }
}

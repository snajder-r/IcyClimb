using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public abstract class LodgeAbleGrabbable : DropablePully
{
    [SerializeField] protected XRSocketInteractor[] returnToHolsterList;
    [SerializeField] protected bool remainsLodgedIfReleased;

    public Rigidbody rigidBody { get; private set; }

    /// <summary>
    /// Whether the object is currently lodged into ice.
    /// </summary>
    [SerializeField] public bool isLodged;

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        if (isLodged)
        {
            if(interactor is XRSocketInteractor)
            {
                // Don't accidentally get pulled into a socket while lodged!
                return false;
            }
        }
        return base.IsSelectableBy(interactor);
    }

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

    private XRSocketInteractor GetFreeHolster()
    {
        foreach (XRSocketInteractor holster in returnToHolsterList)
        {
            if (holster.interactablesSelected.Count == 0)
            {
                return holster;
            }
        }
        return null;
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

        if (!Dislodge()) return;

        XRSocketInteractor holster = GetFreeHolster();
        if (holster is null) return;

        holster.StartManualInteraction((IXRSelectInteractable)this);
    }
    public virtual bool Lodge()
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
        return true;
    }
    public virtual bool Dislodge()
    {
        if (!isLodged)
        {
            return true;
        }

        // Unfreeze the pick from its position.
        rigidBody.constraints = RigidbodyConstraints.None;

        // Play haptics
        if (heldController)
        {
            heldController.SendHapticImpulse(0.1f, 0.1f);
        }

        isLodged = false;
        return true;
    }



    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        Invoke("GoBackToHolster", 1.1f);
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        Dislodge();
    }

    public override void OnOutOfStamina()
    {
        base.OnOutOfStamina();
        Dislodge();
    }
}

public abstract class DropablePully : DropableGrabable, IPullProvider
{
    [SerializeField] private AudioClip[] handSlipSound;
    public abstract Vector3 GetPull();
    public abstract bool IsSecured();

    public virtual void OnOutOfStamina()
    {
        ForceRelease(1f);
        if (heldController)
        {
            AudioSource audio = heldController.gameObject.GetComponent<AudioSource>();
            if (audio)
            {
                int index = Random.Range(0, handSlipSound.Length);
                audio.PlayOneShot(handSlipSound[index], 0.5f);
            }
        }
    }


}

public class DropableGrabable : XRGrabInteractable
{
    protected XRBaseController heldController;

    protected bool grabDisabled = false;

    protected override void Awake()
    {
        base.Awake();
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return !grabDisabled && base.IsSelectableBy(interactor);
    }
    protected void ReenableGrab()
    {
        grabDisabled = false;
    }

    public void ForceRelease(float renableInSeconds)
    {
        grabDisabled = true;
        // One second cooldown before we can grab again
        Invoke("ReenableGrab", renableInSeconds);
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
        }
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);

        if (args.interactorObject is XRBaseControllerInteractor)
        {
            XRBaseController controller = ((XRBaseControllerInteractor)args.interactorObject).xrController;
            controller.SendHapticImpulse(0.15f, 0.075f);
        }
    }

}
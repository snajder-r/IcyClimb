using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This abstract class extends DropablePully - and thus XRGrabInteractable - to describe all objects which can me grabbed and then lodged into the ice
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class LodgeAbleGrabbable : DropablePully
{
    /// <summary>
    /// Whether the object is currently lodged into ice.
    /// </summary>
    [SerializeField]
    public bool IsLodged;

    /// <summary>
    /// List of Sockets to which this item can return to if it is dropped.
    /// It will attempt the sockets in order and pick the first unoccupied one
    /// </summary>
    [SerializeField]
    protected XRSocketInteractor[] _returnToHolsterList;

    /// <summary>
    /// Whether the object will remain lodged in the wall if the grip is released
    /// </summary>
    [SerializeField]
    protected bool _remainsLodgedIfReleased;

    /// <summary>
    /// The rigidbody of this object
    /// </summary>
    public Rigidbody LodgeAbleRigidbody { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        LodgeAbleRigidbody = GetComponent<Rigidbody>();
    }

    public override void OnOutOfStamina()
    {
        base.OnOutOfStamina();
        Dislodge();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        // Return to holster after having the grip released
        Invoke(nameof(GoBackToHolster), 1.1f);
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        // Dislodge when the activation button is pressed
        Dislodge();
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        if (IsLodged && interactor is XRSocketInteractor)
        {
            // Don't accidentally get pulled into a socket while lodged!
            return false;
        }
        return base.IsSelectableBy(interactor);
    }
    /// <summary>
    /// Send a haptic impulse to the controller belonging to the hand which is holding this object
    /// </summary>
    public void SendHapticImpulse(float intensity, float duration)
    {
        if (_heldController)
        {
            _heldController.SendHapticImpulse(intensity, duration);
        }
    }

    /// <summary>
    /// Lodge the object, thus constraining its movement
    /// </summary>
    /// <returns>Whether lodging was successful</returns>
    public virtual bool Lodge()
    {
        // First, freeze the pick in its position.
        LodgeAbleRigidbody.constraints = RigidbodyConstraints.FreezeAll;

        // Play haptics
        if (_heldController)
        {
            _heldController.SendHapticImpulse(1f, 0.25f);
        }

        // Record that it's lodged
        IsLodged = true;
        return true;
    }


    /// <summary>
    /// Dislodge the object, thus freeing its movement from any constraints
    /// </summary>
    /// <returns>Whether dislodging was successful</returns>
    public virtual bool Dislodge()
    {
        if (!IsLodged)
        {
            return true;
        }

        // Unfreeze the object from its position.
        LodgeAbleRigidbody.constraints = RigidbodyConstraints.None;

        // Play haptics
        if (_heldController)
        {
            _heldController.SendHapticImpulse(0.1f, 0.1f);
        }

        IsLodged = false;
        return true;
    }


    /// <summary>
    /// Select the first free holster iterating the list of holsters in order
    /// </summary>
    /// <returns>First available holster or none if there are no free holsters </returns>
    private XRSocketInteractor GetFreeHolster()
    {
        foreach (XRSocketInteractor holster in _returnToHolsterList)
        {
            if (holster.interactablesSelected.Count == 0)
            {
                return holster;
            }
        }
        return null;
    }

    /// <summary>
    /// Return this object to a holster
    /// </summary>
    protected virtual void GoBackToHolster()
    {
        if (this.isSelected)
        {
            //Don't return to holster if the player re-established the grip
            return;
        }

        if (_remainsLodgedIfReleased && IsLodged)
        {
            // Don't return to holster if this object is meant to stay lodged
            return;
        }

        // Don't return to holster if for some reason we failed to dislodge it
        if (!Dislodge()) return;

        XRSocketInteractor holster = GetFreeHolster();
        // Do nothing if there simply isn't any free holster
        if (holster is null) return;

        // Now force the holster to select this object
        holster.StartManualInteraction((IXRSelectInteractable)this);
    }
}

/// <summary>
/// Base class for combining the functions of a dropable grabable and a pully
/// </summary>
public abstract class DropablePully : DropableGrabable, IPullProvider
{
    [Tooltip("Sounds to play when running out of stamina")]
    [SerializeField]
    private AudioClip[] _handSlipSound;
    public abstract Vector3 Pull();
    public abstract bool IsSecured { get; }

    public virtual void OnOutOfStamina()
    {
        // Drop the object when we run out of stamina
        ForceRelease(1f);
        if (_heldController)
        {
            // Play a sound effect indicating that we were forced to drop the object
            AudioSource audio = _heldController.gameObject.GetComponent<AudioSource>();
            if (audio)
            {
                int index = Random.Range(0, _handSlipSound.Length);
                audio.PlayOneShot(_handSlipSound[index], 0.5f);
            }
        }
    }
}

/// <summary>
/// An XRGrabInteractable which can be manually dropped
/// </summary>
public class DropableGrabable : XRGrabInteractable
{
    /// <summary>
    /// The controller grabbing the object
    /// </summary>
    protected XRBaseController _heldController;

    /// <summary>
    /// Whether grabbing is temporarily disabled
    /// </summary>
    protected bool _isGrabDisabled = false;

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        // Store which controller holds it, in order to use haptics
        if (args.interactorObject is XRBaseControllerInteractor interactor)
        {
            _heldController = interactor.xrController;
            // Play a small haptic impulse when selected
            _heldController.SendHapticImpulse(0.5f, 0.1f);
        }
    }
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (args.interactorObject is XRBaseControllerInteractor)
        {
            // If it was deselected from hand (not from the holster)
            _heldController = null;
        }
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);

        if (args.interactorObject is XRBaseControllerInteractor interactor)
        {
            // Play a small haptic impulse when hovered
            XRBaseController controller = interactor.xrController;
            controller.SendHapticImpulse(0.15f, 0.075f);
        }
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        // Set it to not selectable when grab is disabled, thus forcing it to be dropped
        return !_isGrabDisabled && base.IsSelectableBy(interactor);
    }

    /// <summary>
    /// Force the interactor to drop this interactable
    /// </summary>
    /// <param name="renableInSeconds">How long grabbing the object should remain impossible</param>
    public void ForceRelease(float renableInSeconds)
    {
        _isGrabDisabled = true;

        // One second cooldown before we can grab again
        Invoke(nameof(ReenableGrab), renableInSeconds);
    }
    protected void ReenableGrab()
    {
        // Re-enable selecting the item so it can be picked up again after it was forcefully dropped
        _isGrabDisabled = false;
    }

}
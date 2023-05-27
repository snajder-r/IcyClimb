using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class IcePick : XRGrabInteractable
{
    [Header("Ice Pick")]
    [SerializeField] private XRSocketInteractor holster;
    [SerializeField] private AudioClip[] handSlipSound;

    /// <summary>
    /// Whether the Ice pick is currently lodged into ice.
    /// </summary>
    public bool isLodged { get; private set;}
    public Rigidbody rigidBody { get; private set; }

    private XRBaseController heldController;
    private IcePickTip tip;
    private bool grabDisabled = false;

    /// <summary>
    /// The pull the axe currently exacts on the player. 
    /// </summary>
    public Vector3 PullPlayer
    {
        get {
            if (!(isLodged && isSelected))
            {
                // Only pull the player if the ice pick is lodged in ice and held by the player
                return Vector3.zero;
            }
            return attachTransform.position - heldController.transform.position;
        }
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return !grabDisabled && base.IsSelectableBy(interactor);
    }

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        tip = GetComponentInChildren<IcePickTip>();
    }

    public void Lodge()
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

        // Fire listeners
        PlayerController.instance.OnPickLodged(this);
    }

    public void Dislodge()
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

        // Fire listeners
        tip.OnDislodge();
        PlayerController.instance.OnPickDislodged(this);
    }

    public void SendHapticImpulse(float intensity, float duration)
    {
        if (heldController) {
            heldController.SendHapticImpulse(intensity, duration);
        }
    }

    public void LoseGrip()
    {
        grabDisabled = true;
        if (heldController)
        {
            AudioSource audio = heldController.gameObject.GetComponent<AudioSource>();
            if (audio)
            {
                int index = Random.Range(0, handSlipSound.Length);
                audio.PlayOneShot(handSlipSound[index], 0.5f);
            }
        }
        
        // One second cooldown before we can grab again
        Invoke("ReenableGrab", 1f);
    }

    public void ReenableGrab()
    {
        grabDisabled = false;
    }

    private void GoBackToHolster()
    {
        if (this.isSelected)
        {
            //Don't return to holster if the player re-established the grip
            return;
        }

        Dislodge();
        holster.StartManualInteraction((IXRSelectInteractable) this);
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

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        Dislodge();
    }
}

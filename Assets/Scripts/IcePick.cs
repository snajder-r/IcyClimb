using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class IcePick : XRGrabInteractable
{
    [Header("Ice Pick")]
    [SerializeField] private XRSocketInteractor holster;

    /// <summary>
    /// Whether the Ice pick is currently lodged into ice.
    /// </summary>
    public bool lodged { get; private set;}
    public Rigidbody rigidBody { get; private set; }

    private XRBaseController m_HeldController;
    private IcePickTip m_Tip;
 
    /// <summary>
    /// The pull the axe currently exacts on the player. 
    /// </summary>
    public Vector3 PullPlayer
    {
        get {
            if (!(lodged && isSelected))
            {
                // Only pull the player if the ice pick is lodged in ice and held by the player
                return Vector3.zero;
            }
            return attachTransform.position - m_HeldController.transform.position;
        }
    }

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        m_Tip = GetComponentInChildren<IcePickTip>();
    }

    public void Lodge()
    {
        // First, freeze the pick in its position.
        rigidBody.constraints = RigidbodyConstraints.FreezeAll;

        // Play haptics
        if (m_HeldController)
        {
            m_HeldController.SendHapticImpulse(1f, 0.25f);
        }

        // Record that it's lodged
        lodged = true;

        // Fire listeners
        PlayerController.instance.OnPickLodged(this);
    }

    public void Dislodge()
    {
        if (!lodged)
        {
            return;
        }

        // Unfreeze the pick from its position.
        rigidBody.constraints = RigidbodyConstraints.None;

        // Play haptics
        if (m_HeldController)
        {
            m_HeldController.SendHapticImpulse(0.1f, 0.1f);
        }

        lodged = false;

        // Fire listeners
        m_Tip.OnDislodge();
        PlayerController.instance.OnPickDislodged(this);
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
            m_HeldController = null;
            Invoke("GoBackToHolster", 3f);
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        // Store which controller holds it, in order to use haptics
        if (args.interactorObject is XRBaseControllerInteractor)
        {
            m_HeldController = ((XRBaseControllerInteractor)args.interactorObject).xrController;
            m_HeldController.SendHapticImpulse(0.5f, 0.1f);
        }
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        Dislodge();
    }
}

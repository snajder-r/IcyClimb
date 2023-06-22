using UnityEngine;

/// <summary>
/// A wall anchor that can hold a chain link in order to secure the climber
/// </summary>
public class WallAnchor : LodgeAbleGrabbable, IWallTriggerCollider
{
    [SerializeField]
    Transform _ropeAttachPoint;
    [SerializeField]
    AudioClip _lodgeSound;
    [SerializeField]
    AudioClip _dislodgeSound;

    Rope _rope;
    AudioSource _audioSource;

    public ChainLink _link { get; private set; }

    public override bool IsSecured { get => false; }

    void Start()
    {
        _rope = Rope.instance;
        _remainsLodgedIfReleased = true;
        _audioSource = GetComponent<AudioSource>();
    }

    public void OnWallCollisionEnter(Collider cliff)
    {
        // It can only be lodged by hand
        if (_heldController == null) return;
        if (Lodge())
        {
            PlayLodgeSound();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        // Only do something if we are in a wall
        if (!IsLodged) return;

        // If we already have a link attached, ignore the trigger
        if (_link) return;

        // Get the manipulator that has been used to move the link towards us.
        RopeManipulation manipulator = other.GetComponent<RopeManipulation>();
        // This also ensures that we don't just catch the rope by chance
        if (!manipulator) return;

        // Only a manipulator that is selected (held in hand) should trigger me
        if (!manipulator.isSelected) return;

        // Get the link which the player is trying to attach to us
        _link = manipulator.ManipulatorChainLink;
        if (!_link) return;

        // Inform the rope manipulator that we will take responsibility for this chain link
        manipulator.OnRopeAttached();
        // Take over parenthood
        _link.transform.parent = transform;

        ChainLink insertPoint = manipulator.GetChainLinkToPrependNewWallAnchors();
        insertPoint.InsertBefore(_link);
        _link.gameObject.SetActive(true);

        _link.transform.position = _ropeAttachPoint.position;

        //Tell the player that we are secured
        _rope.WallAnchorSecured(this);
    }

    public override bool Dislodge()
    {
        if (_link is not null)
        {
            // We currently have a rope attached
            if (_rope.IsWallAnchorLastInChain(this))
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
        _rope.WallAnchorRemoved(this);
        _link.DisconnectSelf();
        Destroy(_link.gameObject);
        _link = null;
    }
    public override Vector3 Pull() => Vector3.zero;

    private void PlayLodgeSound()
    {
        _audioSource.PlayOneShot(_lodgeSound);
    }

    private void PlayDisLodgeSound()
    {
        _audioSource.PlayOneShot(_dislodgeSound);
    }

    
}

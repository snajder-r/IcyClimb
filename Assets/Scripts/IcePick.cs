using UnityEngine;

/// <summary>
/// The lodgeable grabbable which represents the player's ice pick
/// </summary>
public class IcePick : LodgeAbleGrabbable
{
    [SerializeField] float _pullSpeedModifier = 1f;

    IcePickTip _icePickTip;

    public override bool IsSecured { get => IsLodged && isSelected; }


    void Start()
    {
        // The ice does not stay lodged in the wall if we release it - it returns to the belt
        _remainsLodgedIfReleased = false;
        _icePickTip = GetComponentInChildren<IcePickTip>();
    }

    /// <summary>
    /// The pull the axe currently exacts on the player. 
    /// </summary>
    public override Vector3 Pull()
    {
        if (!IsSecured)
        {
            // Only pull the player if the ice pick is lodged in ice and held by the player
            return Vector3.zero;
        }
        return (attachTransform.position - _heldController.transform.position) * _pullSpeedModifier;
    }

    public override bool Dislodge()
    {
        if (base.Dislodge())
        {
            // Let the ice pick tip know that we dislodged from the wall
            _icePickTip.OnDislodge();
            return true;
        }
        return false;
    }
}

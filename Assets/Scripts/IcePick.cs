using UnityEngine;

public class IcePick : LodgeAbleGrabbable
{
    [SerializeField] float PullSpeedModifier = 1f;
    public override bool IsSecured() => isLodged && isSelected;

    /// <summary>
    /// The pull the axe currently exacts on the player. 
    /// </summary>
    public override Vector3 GetPull(){ 
        if (!IsSecured())
        {
            // Only pull the player if the ice pick is lodged in ice and held by the player
            return Vector3.zero;
        }
        return (attachTransform.position - heldController.transform.position) * PullSpeedModifier;
    }

    void Start()
    {
        remainsLodgedIfReleased = false;
    }
}

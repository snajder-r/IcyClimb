using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Break : MonoBehaviour
{
    [SerializeField] XRGrabInteractable grabInteractible;
    [SerializeField] Transform breakingDeviceModel;
    [SerializeField] Transform hingePoint;
    [SerializeField] ChainLink ropeIn;
    [SerializeField] Rope rope;

    [SerializeField] float maxYAngle;
    [SerializeField] float minYAngle;
    [SerializeField] float maxXAngle;
    [SerializeField] float minXAngle;

    [ShowOnly][SerializeField]
    public bool BreakEngaged;

    public int ManualEngageBreak { get; set; } = 0;
    public bool ManualReleaseBreak { get; set; }

    // Update is called once per frame
    void Update()
    {
        Vector3 towardsPosition;
        if (grabInteractible.isSelected)
        {
            towardsPosition = grabInteractible.transform.position;
        }
        else
        {
            towardsPosition = ropeIn.previousLink.transform.position;
        }

        Vector3 direction = towardsPosition - hingePoint.position;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        rotation = ClampRotation(rotation);

        Vector3 offsetPosition = Quaternion.Inverse(breakingDeviceModel.transform.rotation)*(breakingDeviceModel.transform.position - hingePoint.position);
        breakingDeviceModel.transform.position = rotation * offsetPosition + hingePoint.position;
        breakingDeviceModel.transform.rotation = rotation;

        // Lock the break if the rope is above, unless we are manually activated or deactivated
        if (ManualEngageBreak>0)
        {
            BreakEngaged = true;
        }
        else if (ManualReleaseBreak)
        {
            BreakEngaged = false;
        }
        else
        {
            BreakEngaged = towardsPosition.y > transform.position.y;
        }
        
        rope.IsExtendingRope = !BreakEngaged;
    }


    Quaternion ClampRotation(Quaternion rotation)
    {
        Vector3 eulerHinge = hingePoint.eulerAngles;
        Vector3 eulerRotation = rotation.eulerAngles;
        Vector3 eulerDiff = Vector3.zero;
        eulerDiff.x = Mathf.DeltaAngle(eulerHinge.x, eulerRotation.x);
        eulerDiff.y = Mathf.DeltaAngle(eulerHinge.y, eulerRotation.y);
        eulerDiff.z = 0f;

        eulerDiff.y = Mathf.Clamp(eulerDiff.y, minYAngle, maxYAngle);
        eulerDiff.x = Mathf.Clamp(eulerDiff.x, minXAngle, maxXAngle);
        eulerDiff.z = 0f;

        return Quaternion.Euler(eulerHinge + eulerDiff);
    }

}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Behavior of the belay device at the front of the climbing belt, which limits whether
/// a rope can be extended and thus secures the player when falling
/// </summary>
public class BelayDevice : MonoBehaviour
{
    [Tooltip("The grab interactible which the belay device should follow look at")]
    [SerializeField]
    XRGrabInteractable _grabInteractible;
    [Tooltip("Location the belay device")]
    [SerializeField]
    Transform _breakingDeviceModel;
    [Tooltip("Location the belay device rotates around")]
    [SerializeField]
    Transform _hingePoint;
    [SerializeField]
    ChainLink _ropeIn;
    [SerializeField]
    Rope _rope;

    [Header("Movement range in degrees")]
    [SerializeField]
    float _maxYAngle;
    [SerializeField]
    float _minYAngle;
    [SerializeField]
    float _maxXAngle;
    [SerializeField]
    float _minXAngle;

    /// <summary>
    /// Whether the break is currently engaged. 
    /// An engaged break means the rope can no longer be extended (and the player would not fall)
    /// </summary>
    public bool BreakEngaged { get; set; }

    /// <summary>
    /// Number of sources which currently force the break to be manually engaged.
    /// When set to zero, the break is no longer forced engaged
    /// </summary>
    public int NumberOfManualEngageBreak { get; set; } = 0;

    /// <summary>
    /// Whether the Break is manually released by the user (by grabbing the belay device and activating it)
    /// </summary>
    public bool IsManualReleaseBreak { get; set; }

    void Update()
    {
        Vector3 towardsPosition;
        
        if (_grabInteractible.isSelected)
        {
            // Turn the belay device towards the grab interactible if it is held by the player
            towardsPosition = _grabInteractible.transform.position;
        }
        else
        {
            // Or else towards the next rope link
            towardsPosition = _ropeIn.PreviousLink.transform.position;
        }

        // Rotate the belay device around the hinge position.
        Vector3 direction = towardsPosition - _hingePoint.position;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        rotation = ClampRotation(rotation);

        Vector3 offsetPosition = Quaternion.Inverse(_breakingDeviceModel.transform.rotation)*(_breakingDeviceModel.transform.position - _hingePoint.position);
        _breakingDeviceModel.transform.position = rotation * offsetPosition + _hingePoint.position;
        _breakingDeviceModel.transform.rotation = rotation;

        // Lock the break if the rope is above, unless we are manually activated or deactivated
        if (NumberOfManualEngageBreak>0)
        {
            BreakEngaged = true;
        }
        else if (IsManualReleaseBreak)
        {
            BreakEngaged = false;
        }
        else
        {
            BreakEngaged = towardsPosition.y > transform.position.y;
        }

        // Tell the rope whether it can be extended, based on whether the break is currently engaged
        _rope.IsExtendingRope = !BreakEngaged;
    }

    /// <summary>
    /// Clamp rotation based on the min and max angles set in the Inspector
    /// </summary>
    /// <param name="rotation">The rotation to clamp</param>
    /// <returns>The clamped rotation</returns>
    Quaternion ClampRotation(Quaternion rotation)
    {
        Vector3 eulerHinge = _hingePoint.eulerAngles;
        Vector3 eulerRotation = rotation.eulerAngles;
        // First determine the target rotation relative to the rotation of the hingePoint
        Vector3 eulerDiff = Vector3.zero;
        eulerDiff.x = Mathf.DeltaAngle(eulerHinge.x, eulerRotation.x);
        eulerDiff.y = Mathf.DeltaAngle(eulerHinge.y, eulerRotation.y);
        // We are only interested in x (up/down) and y (left/right) rotation, so we set z to 0
        eulerDiff.z = 0f;

        // Now clamp and return the rotation
        eulerDiff.y = Mathf.Clamp(eulerDiff.y, _minYAngle, _maxYAngle);
        eulerDiff.x = Mathf.Clamp(eulerDiff.x, _minXAngle, _maxXAngle);
        eulerDiff.z = 0f;

        // The target rotation is the hinge rotation plus the clamped relative rotation
        return Quaternion.Euler(eulerHinge + eulerDiff);
    }

}

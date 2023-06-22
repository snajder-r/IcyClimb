using UnityEngine;

/// <summary>
/// Used to reset the object to its local position either on update or when a trigger occurs
/// </summary>
public class ResetLocalPosition : MonoBehaviour
{
    [SerializeField, Tooltip("The reference to reset the position to. If left empty, position will be reset to the parent.")] 
    Transform _reference;

    /// <summary>
    /// If true, the position will not be reset in the update and only reset when SetPositionToZero is called
    /// </summary>
    bool _holdPosition;

    private void Update()
    {
        if (!_holdPosition)
        {
            SetPositionToZero();
        }
    }
    /// <summary>
    /// Set whether to hold the position
    /// </summary>
    /// <param name="hold">If true, the position will not be reset in the update and only reset when SetPositionToZero is called</param>
    public void SetHoldPosition(bool hold)
    {
        _holdPosition = hold;
    }

    /// <summary>
    /// Force resetting the position, even when hold position is set
    /// </summary>
    public void SetPositionToZero()
    {
        if (_reference)
        {
            transform.SetPositionAndRotation(_reference.position, _reference.rotation);
        }
        else { 
            transform.localPosition = Vector3.zero;
        }
    }
}

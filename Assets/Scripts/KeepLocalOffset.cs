using UnityEngine;

/// <summary>
/// Use this behavior if you want the object to follow the parents location and Y rotation, but not the XZ rotation
/// </summary>
public class KeepLocalOffset : MonoBehaviour
{
    Vector3 _offset;

    void Start()
    {
        _offset = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        // Follow Y rotation only
        Vector3 forward = Vector3.ProjectOnPlane(transform.parent.forward, Vector3.up);
        transform.SetPositionAndRotation(transform.parent.position + _offset, Quaternion.LookRotation(forward));
    }
}

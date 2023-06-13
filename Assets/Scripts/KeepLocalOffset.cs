using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepLocalOffset : MonoBehaviour
{
    private Vector3 offset;
    private Quaternion rotation;

    void Start()
    {
        offset = transform.localPosition;
        rotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Follow Y rotation only
        Vector3 forward = Vector3.ProjectOnPlane(transform.parent.forward, Vector3.up);
        transform.position = transform.parent.position + offset;
        transform.rotation = Quaternion.LookRotation(forward);
    }
}

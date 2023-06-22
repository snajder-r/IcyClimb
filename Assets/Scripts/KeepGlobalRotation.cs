using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Use this behavior if you want an object to follow its parents position, but keep its global starting rotation
/// </summary>
public class KeepGlobalRotation : MonoBehaviour
{
    Quaternion _rotation;

    void Start()
    {
        _rotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = _rotation;
    }
}

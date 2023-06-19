using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetLocalPosition : MonoBehaviour
{
    [SerializeField] Transform reference;
    
    bool holdPosition;

    public void SetHoldPosition(bool hold)
    {
        holdPosition = hold;
    }

    private void Update()
    {
        if (!holdPosition)
        {
            SetPositionToZero();
        }
    }

    public void SetPositionToZero()
    {
        if (reference)
        {
            transform.position = reference.position;
            transform.rotation = reference.rotation;
        }
        else { 
            transform.localPosition = Vector3.zero;
        }
    }
}

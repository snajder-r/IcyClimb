using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetLocalPosition : MonoBehaviour
{
    public void SetPositionToZero()
    {
        transform.localPosition = Vector3.zero;
    }
}

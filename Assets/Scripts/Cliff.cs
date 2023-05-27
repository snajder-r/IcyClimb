using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cliff : MonoBehaviour
{
    private Collider cliffCollider;

    void Start()
    {
        cliffCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("IcePickTip"))
        {
            other.GetComponent<IcePickTip>().OnWallCollisionEnter(cliffCollider);
        }
        if (other.CompareTag("WallAnchor"))
        {
            other.GetComponent<WallAnchor>().OnWallCollisionEnter(cliffCollider);
        }
    }
}

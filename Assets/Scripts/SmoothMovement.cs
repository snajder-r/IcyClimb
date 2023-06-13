using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothMovement : MonoBehaviour
{
    [SerializeField] int numberOfFrames;


    SmoothedVector3 smoothedParentPosition;

    Vector3 localPosition;

    void Start()
    {
        smoothedParentPosition = new SmoothedVector3(numberOfFrames);
        for(int i = 0; i < numberOfFrames; i++) {
            smoothedParentPosition.Add(transform.parent.position);
        }
        localPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        
        smoothedParentPosition.Add(transform.parent.position);
        transform.position = smoothedParentPosition.Mean + localPosition;
    }

    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RayTraceOcclusion : MonoBehaviour
{
    [SerializeField] Component componentToHide;
    [SerializeField] Transform mainCamera;
    [SerializeField] float minDistance;
    [SerializeField] LayerMask wallMask;

    bool IsOccluded()
    {
        Vector3 offset = transform.position - mainCamera.position;
        if (offset.magnitude > minDistance)
        {
            return true;
        }
        bool hit = Physics.Raycast(mainCamera.position, offset.normalized, offset.magnitude, wallMask);
        return hit;
    }

    // Update is called once per frame
    void Update()
    {
        bool occluded = IsOccluded();
        if(componentToHide is LensFlareComponentSRP)
        {
            ((LensFlareComponentSRP)componentToHide).enabled = !occluded;
        }else if (componentToHide is Renderer)
        {
            ((Renderer)componentToHide).enabled = !occluded;
        }
    }

    
}

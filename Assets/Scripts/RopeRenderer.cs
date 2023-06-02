using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class RopeRenderer : MonoBehaviour
{

    [SerializeField] private ChainLink ropeAnchor;
    [SerializeField] private TrailRenderer trail;



    void Start()
    {      
        // This is to avoid that the renderer gets frustum culled. Making its bounds very large so it's always in the camera frustum!
        trail.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
    }

    void Update()
    {
        RenderRope();

        
    }

    void RenderRope()
    {
        Vector3[] positions = GetRopePositions().Reverse().ToArray();
        if(positions.Length == trail.positionCount)
        {
            // If the number of chain links is unchanged, setting the positions is the cheapest option
            trail.SetPositions(positions);
        }
        else if(positions.Length > trail.positionCount)
        {
            // If the number of chain links has increased, set the existing positions and add the additional ones
            trail.SetPositions(new ArraySegment<Vector3>(positions, 0, trail.positionCount).ToArray());
            int remainder = positions.Length - trail.positionCount;
            trail.AddPositions(new ArraySegment<Vector3>(positions, trail.positionCount, remainder).ToArray());
        }
        else
        {
            // If the number of chain links has decreased, clear the entire trail
            trail.Clear();
            trail.AddPositions(positions);
        }
    }

    IEnumerable<Vector3> GetRopePositions()
    {
        ChainLink link = ropeAnchor;
        while (true)
        {
            yield return link.transform.position;
            link = link.nextLink;
            if (!link) break;
        }
        
    }

}

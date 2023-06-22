using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;
using System;

/// <summary>
/// Renders a ChainLink based rope using a TrailRenderer
/// </summary>
public class RopeRenderer : MonoBehaviour
{
    [SerializeField, Tooltip("Start point of the rope")] 
    private ChainLink _ropeAnchor;

    [SerializeField] 
    private TrailRenderer _trail;

    void Start()
    {      
        // This is to avoid that the renderer gets frustum culled. Making its bounds very large so it's always in the camera frustum!
        _trail.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
    }

    void LateUpdate()
    {
        RenderRope();  
    }

    void RenderRope()
    {
        Vector3[] positions = GetRopePositions().Reverse().ToArray();
        if(positions.Length == _trail.positionCount)
        {
            // If the number of chain links is unchanged, setting the positions is the cheapest option
            _trail.SetPositions(positions);
        }
        else if(positions.Length > _trail.positionCount)
        {
            // If the number of chain links has increased, set the existing positions and add the additional ones
            _trail.SetPositions(new ArraySegment<Vector3>(positions, 0, _trail.positionCount).ToArray());
            int remainder = positions.Length - _trail.positionCount;
            _trail.AddPositions(new ArraySegment<Vector3>(positions, _trail.positionCount, remainder).ToArray());
        }
        else
        {
            // If the number of chain links has decreased, clear the entire trail
            _trail.Clear();
            _trail.AddPositions(positions);
        }
    }

    /// <summary>
    /// Generates positions of chainlinks in order
    /// </summary>
    IEnumerable<Vector3> GetRopePositions()
    {
        ChainLink link = _ropeAnchor;
        while (true)
        {
            yield return link.transform.position;
            link = link.NextLink;
            if (!link) break;
        }        
    }
}

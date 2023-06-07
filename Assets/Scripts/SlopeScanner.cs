using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlopeScanner : MonoBehaviour
{
    // Raycastings used to determine whether we should fall
    Vector3[] fallRays;

    [SerializeField]
    int numberOfRays;

    [SerializeField]
    LayerMask floorLayerMask;

    [SerializeField]
    float floorAngleSlipping = 95f;


    /// <param name="floorNormal">
    /// Returns the average floor normal below the player based on a 
    /// number of rays around the player. Can be used to determine whether the
    /// player stands on solid ground or a slope. 
    /// If player is in mid-air, returns -Vector.up
    /// </param>
    /// <returns>
    /// Returns true if player stands on solid ground
    /// </returns>
    public bool ScanFallRays(out Vector3 floorNormal)
    {
        Vector3 normalSum = Vector3.zero;
        foreach (Vector3 offset in fallRays)
        {
            RaycastHit hit;
            Vector3 normal = -Vector3.up;
            if (Physics.Raycast(transform.position, offset, out hit, 2f, floorLayerMask))
            {
                normal = hit.normal;
            }
            normalSum += normal;
        }
        floorNormal = normalSum / fallRays.Length;

        float floorAngle = Vector3.Dot(floorNormal, Vector3.up);
        return Mathf.Acos(floorAngle) * Mathf.Rad2Deg <= floorAngleSlipping;
    }

    void Start()
    {
        SetUpFallRays();
    }

    void SetUpFallRays()
    {
        float stepDegrees = 360f / numberOfRays;
        List<Vector3> rays = new List<Vector3>();
        Quaternion rotation = Quaternion.AngleAxis(30f, Vector3.up);
        //Start with one ray, and then rotate it around the y axis to create a higher resolution scan
        rays.Add(new Vector3(0.1f, -0.1f, 0f));
        for (int i = 1; i < 360f / stepDegrees; i++)
        {
            rays.Add(rotation * rays[rays.Count - 1]);
        }
        fallRays = rays.ToArray();
    }
}

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
    Transform yTransform;

    [SerializeField]
    float floorAngleSlipping = 95f;

    [SerializeField] 
    float angle;

    [SerializeField]
    float rayLength;

    [ShowOnly][SerializeField] Vector3 lastScannedNormal;
    [ShowOnly] [SerializeField] float lastScannedAngle;

    void Update()
    {
        transform.position = new Vector3(transform.position.x, yTransform.position.y + 0.1f, transform.position.z);
    }

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
            if (Physics.Raycast(transform.position, offset, out hit, rayLength, floorLayerMask))
            {
                normal = hit.normal;
            }
            normalSum += normal;
        }
        floorNormal = normalSum / fallRays.Length;
        lastScannedNormal = floorNormal;
        float floorAngle = Vector3.Dot(floorNormal, Vector3.up);
        lastScannedAngle = Mathf.Acos(floorAngle) * Mathf.Rad2Deg;
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
        Quaternion rotation = Quaternion.AngleAxis(stepDegrees, Vector3.up);
        //Start with one ray, and then rotate it around the y axis to create a higher resolution scan
        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * (-Vector3.up);
        rays.Add(direction);
        for (int i = 1; i < 360f / stepDegrees; i++)
        {
            rays.Add(rotation * rays[rays.Count - 1]);
        }
        fallRays = rays.ToArray();
    }
}

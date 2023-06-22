using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// This SlopeScanner can help detect how even the floor below the player is and decides whether the player should be able
/// to walk or might slip and fall.
/// </summary>
public class SlopeScanner : MonoBehaviour
{
    // Raycastings used to determine whether we should fall
    Vector3[] _fallRays;

    [SerializeField, Tooltip("Number of raycasts which should be performed in a circle around this object")]
    int _numberOfRays;

    [SerializeField, Tooltip("Which layers should be considered a floor")]
    LayerMask _floorLayerMask;

    [SerializeField, Tooltip("Which transform we should follow to determine the y-coordinate of the scan. This is important so taller people don't fall just because they are taller than the rays are long.")]
    Transform _yTransform;

    [SerializeField, Tooltip("A lower angle means that the player is more likely to slide down a slope. The angle is represented in degrees.")]
    float _floorAngleSlipping = 95f;

    [SerializeField, Tooltip("The angle at which rays are being fired towards the floor. 90 would be shooting rays sideways (not a good idea) and 0 would mean all rays are shot in the same direction straight down.")] 
    float _angle;

    [SerializeField]
    float _rayLength;

    [SerializeField, ShowOnly] 
    Vector3 _lastScannedNormal;
    [SerializeField, ShowOnly] 
    float _lastScannedAngle;

    void Update()
    {
        transform.position = new Vector3(transform.position.x, _yTransform.position.y + 0.1f, transform.position.z);


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
        Gradient gradient;
        GradientColorKey[] colorKey;
        GradientAlphaKey[] alphaKey;
        gradient = new Gradient();

        // Populate the color keys at the relative time 0 and 1 (0 and 100%)
        colorKey = new GradientColorKey[2];
        colorKey[0].color = Color.green;
        colorKey[0].time = 0f;
        colorKey[1].color = Color.red;
        colorKey[1].time = 1f;

        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1f;

        gradient.SetKeys(colorKey, alphaKey);

        Vector3 normalSum = Vector3.zero;
        foreach (Vector3 offset in _fallRays)
        {
            Vector3 normal = -Vector3.up;
            

            if (Physics.Raycast(transform.position, offset, out RaycastHit hit, _rayLength, _floorLayerMask))
            {
                normal = hit.normal;

                float tmpangle = Vector3.Dot(normal, Vector3.up);
                tmpangle = Mathf.Acos(tmpangle) * Mathf.Rad2Deg;
                tmpangle = tmpangle / _floorAngleSlipping;
                Debug.DrawLine(transform.position, hit.point, gradient.Evaluate(tmpangle));
            }
            else
            {
                Debug.DrawLine(transform.position, transform.position + offset * _rayLength, gradient.Evaluate(0f));
            }
            normalSum += normal;
        }
        floorNormal = normalSum / _fallRays.Length;
        _lastScannedNormal = floorNormal;
        float floorAngle = Vector3.Dot(floorNormal, Vector3.up);
        _lastScannedAngle = Mathf.Acos(floorAngle) * Mathf.Rad2Deg;
        return _lastScannedAngle <= _floorAngleSlipping;
    }

    void Start()
    {
        SetUpFallRays();
    }

    void SetUpFallRays()
    {
        float stepDegrees = 360f / _numberOfRays;
        List<Vector3> rays = new();
        Quaternion rotation = Quaternion.AngleAxis(stepDegrees, Vector3.up);
        //Start with one ray, and then rotate it around the y axis to create a higher resolution scan
        Vector3 direction = Quaternion.AngleAxis(_angle, Vector3.forward) * (-Vector3.up);

        rays.Add(direction);
        for (int i = 1; i < 360f / stepDegrees; i++)
        {
            rays.Add(rotation * rays[rays.Count - 1]);
        }
        _fallRays = rays.ToArray();
    }
}

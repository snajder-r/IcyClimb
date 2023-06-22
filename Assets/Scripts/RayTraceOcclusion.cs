using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Behavior used to detect whether a component object is visible from the main camera.
/// If not, it will be made invisible. 
/// In the current implementation, component types need to be hardcoded, since not every
/// component has the "enabled" field. Check below to see which are currently supported
/// </summary>
public class RayTraceOcclusion : MonoBehaviour
{
    [SerializeField] Component _componentToHide;
    [SerializeField] Transform _mainCamera;
    [SerializeField] float _minDistance;
    [SerializeField] LayerMask _wallMask;

    /// <summary>
    /// Check if the object is occluded, by sending a raycast from the camera to the object
    /// </summary>
    /// <returns>true if the raycast hit a wall before reaching the object</returns>
    bool IsOccluded()
    {
        Vector3 offset = transform.position - _mainCamera.position;
        if (offset.magnitude > _minDistance)
        {
            return true;
        }
        bool hit = Physics.Raycast(_mainCamera.position, offset.normalized, offset.magnitude, _wallMask);
        return hit;
    }

    void Update()
    {
        if(!_componentToHide)
        {
            Debug.LogError("No component provided for " + GetType());
            return;
        }

        bool occluded = IsOccluded();

        // Occlude if the component is of a supported type
        switch (_componentToHide)
        {
            case LensFlareComponentSRP flare: flare.enabled = !occluded; break;
            case Renderer renderer: renderer.enabled = !occluded; break;
            default: Debug.LogError("Component type " + _componentToHide.GetType() + " not supported by " + GetType()); break;
        }
    }
}

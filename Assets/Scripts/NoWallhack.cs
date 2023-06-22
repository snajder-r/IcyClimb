using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fades the camera to black when it goes inside a wall. 
/// Extra checks are in place for non-convex mesh colliders to determine whether the head is inside or outside of the mesh.
/// </summary>
public class NoWallhack : MonoBehaviour
{
    #region "Fields"
    [SerializeField, Tooltip("Distance of colliding collider at which screen is completely blacked out")]
    float _maxBlindfoldDistance = 0.1f;
    [SerializeField, Tooltip("This material needs to contain the PenetrationDepth (float) property")]
    Material _blindfoldMaterial;
    [SerializeField, Tooltip("Between 0 and 1, the amount of screen space the headset sees")]
    Vector2 _headsetPeripheral = new(0.75f, 0.9f);
    [SerializeField, Tooltip("Typically the XR Rig. Required to check whether head has entirely moved through a collider and come out the other side")]
    Transform _characterPosition;
    [SerializeField, Tooltip("Layers which will be recognized as walls that we are not allowed to peek through")]
    LayerMask _layerMask;

    BoxCollider _myCollider;
    /// <summary>
    /// List of colliders we are currently colliding with
    /// </summary>
    readonly List<Collider> _currentWallColliders = new();
    /// <summary>
    /// List of colliders we collided with but apparently never came out of, meaning we completely passed through them.
    /// Note that concave Mesh colliders do not have a concept of being inside the collier if you are no longer touching
    /// their boundary, hence we try to remember here which colliders we penetrated and haven't come out of yet
    /// </summary>
    readonly List<Collider> _fullyPenetratedColliders = new();

    // This is used to fix a bug in ComputePenetration where sometimes it returns 0 for a single frame, causing flickering.
    float _CurrentPenetration = 0f;
    #endregion

    #region "Lifecycle"
    void Awake()
    {
        _myCollider = GetComponent<BoxCollider>();

        // Adjust my collider distance and dimensions for the camera's near clipping plane.
        // Note that this depends on the display's dimensions
        Vector2 rectSize = GetComponent<RectTransform>().rect.size;
        Vector3 colliderSize = new(rectSize.x * _headsetPeripheral.x, rectSize.y * _headsetPeripheral.y, 10);
        _myCollider.size = colliderSize;

        _blindfoldMaterial.SetFloat("_MaxPenetrationDepth", _maxBlindfoldDistance);
    }
    void Update()
    {
        bool isFullyPenetratingWall = _fullyPenetratedColliders.Count > 0;
        bool isCollidingWithWall = _currentWallColliders.Count > 0;
        if (isFullyPenetratingWall)
        {
            UpdateListOfFullyPenetratedColliders();
        }

        if (isCollidingWithWall)
        {
            AdjustBlindfoldVisibility();
        }

        if (!isFullyPenetratingWall && !isCollidingWithWall)
        {
            SetPenetrationDepth(0f, Vector2.zero);
        }
    }
    #endregion

    #region "Events"
    private void OnCollisionEnter(Collision collision)
    {
        _currentWallColliders.Add(collision.collider);
    }
    private void OnCollisionExit(Collision collision)
    {
        if (!CheckIfExitedRightSide(collision.collider))
        {
            //It looks like we went through the collider
            _fullyPenetratedColliders.Add(collision.collider);
        }
        _currentWallColliders.Remove(collision.collider);

        if (_currentWallColliders.Count + _fullyPenetratedColliders.Count == 0)
        {
            SetPenetrationDepth(0f, Vector2.zero);
        }
        else if (_fullyPenetratedColliders.Count != 0)
        {
            SetPenetrationDepth(_maxBlindfoldDistance, Vector2.zero);
        }
    }
    #endregion

    #region "Methods"
    /// <summary>
    /// Tries to determine which collider (of all colliders we are currently touching) we have penetrated the most,
    /// that is which collider would block the most vision.
    /// </summary>
    /// <param name="direction">Output direction to the collider</param>
    /// <returns>Distance to the collider boundary</returns>
    private float GetClosestColliderDistance(out Vector2 direction)
    {
        
        float highestPenetration = float.NegativeInfinity;
        direction = Vector2.zero;

        foreach (Collider collider in _currentWallColliders)
        {
            Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, _myCollider, _myCollider.transform.position, _myCollider.transform.rotation, out Vector3 translate, out float penetration);

            if (penetration > highestPenetration)
            {
                highestPenetration = penetration;
                translate = Quaternion.Inverse(transform.rotation) * translate;
                direction.x = translate.x;
                direction.y = translate.y;
                direction = direction.normalized;
            }
        }
        return highestPenetration;
    }
    /// <summary>
    /// store the current amount of collider penetration and direction and inform the renderer which 
    /// will then eventually draw the blindfold.
    /// </summary>
    /// <param name="penetration">How far inside the collider we are</param>
    /// <param name="penetrationDirection">Direction to the collider</param>
    private void SetPenetrationDepth(float penetration, Vector2 penetrationDirection)
    {
        if (_currentWallColliders.Count > 0 && penetration == 0f)
        {
            // I don't believe that penetration is 0 when we have active collision.
            // This is an issue with Physics.ComputePenetration sometimes returning 0
            // Use cached value instead
            penetration = _CurrentPenetration;
        }
        else
        {
            _CurrentPenetration = penetration;
        }
        // Inform the renderer
        _blindfoldMaterial.SetFloat("_PenetrationDepth", penetration);
        _blindfoldMaterial.SetVector("_Direction", penetrationDirection);
    }
    /// <summary>
    /// Check all the colliders we remembered as fully penetrated, 
    /// check if we are still penetrating them, and update the list accordingly
    /// </summary>
    private void UpdateListOfFullyPenetratedColliders()
    {
        // First store all the colliders which should be removed from the list.
        // Not super efficient, but remember that this is only executed when the user is blinded
        List<Collider> freedCollider = new();
        foreach (Collider collider in _fullyPenetratedColliders)
        {
            if (CheckIfExitedRightSide(collider))
            {
                // If we exited the collider on the correct side (where we entered) then we are allowed to see again
                freedCollider.Add(collider);
            }
        }
        // Now update the list
        foreach (Collider collider in freedCollider)
        {
            _fullyPenetratedColliders.Remove(collider);
        }
        if (_fullyPenetratedColliders.Count == 0)
        {
            SetPenetrationDepth(0f, Vector2.zero);
        }
    }
    /// <summary>
    /// Check all colliders we are currently colliding with or have penetrated and update the blindfold renderer
    /// </summary>
    private void AdjustBlindfoldVisibility()
    {
        float penetration;
        Vector2 penetrationDirection;
        if (_fullyPenetratedColliders.Count > 0)
        {
            // If we fully penetrated any collider, it's easy: We should see nothing
            penetration = _maxBlindfoldDistance;
            penetrationDirection = Vector2.zero;
        }
        else
        {
            // If we are only just colliding with a collider, then  compute how much we should blindfold
            penetration = GetClosestColliderDistance(out penetrationDirection);
        }
        // Now tell the renderer to update the blindfold
        SetPenetrationDepth(penetration, penetrationDirection);
    }
    /// <summary>
    /// This is a heuristic method to check whether we exited a collider on the same side as we entered it.
    /// We want to prevent a player from poking their head through a wall and then emerging on the other side.
    /// The test is based on whether the exit position is visible from the character position.
    /// </summary>
    /// <param name="collider">The collider which we exited</param>
    /// <returns>true if we exited it on the correct side</returns>
    private bool CheckIfExitedRightSide(Collider collider)
    {
        Vector3 rayEmitter = _characterPosition.position;
        Vector3 myPosition = _myCollider.ClosestPointOnBounds(transform.position);
        rayEmitter.y = myPosition.y;

        Vector3 rayDirection = myPosition - rayEmitter;

        bool hit = Physics.Raycast(rayEmitter, rayDirection, out RaycastHit hitInfo, rayDirection.magnitude, _layerMask);

        if (!hit)
        {
            return true;
        }

        // This isn't perfect as it may fail if multiple colliders are very close to each other,
        // but as long as wall colliders of multiple objects aren't very close this will do it
        // Either way, it errs on the side of freeing the user's vision.
        return hitInfo.collider != collider;
    }
    #endregion
}

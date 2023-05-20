using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoWallhack : MonoBehaviour
{
    [Tooltip("Distance of colliding collider at which screen is completely blacked out")]
    [SerializeField] float maxBlindfoldDistance = 0.1f;

    [Tooltip("This material needs to contain the PenetrationDepth (float) property")]
    [SerializeField] Material blindfoldMaterial;


    [Tooltip("Between 0 and 1, the amount of screen space the headset sees")]
    [SerializeField] Vector2 headsetPeripheral = new Vector2(0.75f, 0.9f);
    

    [SerializeField] Transform characterPosition;

    private Camera m_MainCamera;
    private BoxCollider m_MyCollider;
    private List<Collider> m_CurrentWallColliders = new List<Collider>();
    private List<Collider> m_FullyPenetratedColliders = new List<Collider>();
    private LayerMask layerMask;


    // Start is called before the first frame update
    void Awake()
    {
        m_MainCamera = GetComponent<Camera>();
        
        m_MyCollider = GetComponent<BoxCollider>();

        // Adjust my collider distance and dimensions for the camera's near clipping plane.
        // Note that this depends on the display's dimensions
        Vector2 rectSize = GetComponent<RectTransform>().rect.size;
        Vector3 colliderSize = new Vector3(rectSize.x * headsetPeripheral.x, rectSize.y * headsetPeripheral.y, 2);
        m_MyCollider.size = colliderSize;

        layerMask = LayerMask.GetMask(new string[] { "Wall" });
        blindfoldMaterial.SetFloat("_MaxPenetrationDepth", maxBlindfoldDistance);
    }

    void LateUpdate()
    {
        if(m_FullyPenetratedColliders.Count > 0)
        {
            UpdateListOfFullyPenetratedColliders();
        }

        if (m_CurrentWallColliders.Count > 0)
        {
            AdjustBlindfoldVisibility();
        }
    }

    private float GetClosestColliderDistance(out Vector2 direction)
    {
        float highestPenetration = float.NegativeInfinity;
        direction = Vector2.zero;

        foreach (Collider collider in m_CurrentWallColliders)
        {
            Vector3 translate;
            float penetration;
            Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, m_MyCollider, m_MyCollider.transform.position, m_MyCollider.transform.rotation, out translate, out penetration);

            if (penetration > highestPenetration)
            {
                highestPenetration = penetration;
                //direction = transform.position - collider.ClosestPointOnBounds(transform.position);
                translate = Quaternion.Inverse(transform.rotation) * translate;
                direction.x = translate.x;
                direction.y = translate.y;
                // direction = Quaternion.Inverse(transform.rotation) * direction;
                direction = direction.normalized;
                
            }
        }
        return highestPenetration;
    }
    private void SetPenetrationDepth(float penetration, Vector2 penetrationDirection)
    {
        blindfoldMaterial.SetFloat("_PenetrationDepth", penetration);
        blindfoldMaterial.SetVector("_Direction", penetrationDirection);
    }

    private void UpdateListOfFullyPenetratedColliders()
    {
        // Not super efficient, but remember that this is only executed when the user is blinded
        List<Collider> freedCollider = new List<Collider>();
        foreach(Collider collider in m_FullyPenetratedColliders)
        {
            if (CheckIfExitedRightSide(collider))
            {
                freedCollider.Add(collider);
            }
        }
        foreach(Collider collider in freedCollider)
        {
            m_FullyPenetratedColliders.Remove(collider);
        }
        if(m_FullyPenetratedColliders.Count == 0)
        {
            SetPenetrationDepth(0f, Vector2.zero);
        }
    }

    private void AdjustBlindfoldVisibility()
    {
        float penetration;
        Vector2 penetrationDirection;
        if (m_FullyPenetratedColliders.Count > 0)
        {
            penetration = maxBlindfoldDistance;
            penetrationDirection = Vector2.zero;
        }
        else
        {
            penetration = GetClosestColliderDistance(out penetrationDirection);
        }
        SetPenetrationDepth(penetration, penetrationDirection);
    }

    private bool CheckIfExitedRightSide(Collider collider)
    {
        Vector3 rayEmitter = characterPosition.position;
        Vector3 myPosition = m_MyCollider.ClosestPointOnBounds(transform.position);
        rayEmitter.y = myPosition.y;

        Vector3 rayDirection = myPosition - rayEmitter;

        RaycastHit hitInfo;
        bool hit = Physics.Raycast(rayEmitter, rayDirection, out hitInfo, rayDirection.magnitude, layerMask);

        if (!hit)
        {
            return true;
        }

        // This isn't perfect as it may fail if multiple colliders are very close to each other,
        // but as long as wall colliders of multiple objects aren't very close this will do it
        // Either way, it errs on the side of freeing the user's vision.
        return hitInfo.collider != collider;
    }

    private void OnCollisionEnter(Collision collision)
    {
        m_CurrentWallColliders.Add(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!CheckIfExitedRightSide(collision.collider))
        {
            //It looks like we went through the collider
            m_FullyPenetratedColliders.Add(collision.collider);
            m_CurrentWallColliders.Remove(collision.collider);
        }

        if (m_CurrentWallColliders.Count == 0)
        {
            SetPenetrationDepth(0f, Vector2.zero);
        }

        if (m_CurrentWallColliders.Count != 0)
        {
            SetPenetrationDepth(maxBlindfoldDistance, Vector2.zero);
        }
    }
}

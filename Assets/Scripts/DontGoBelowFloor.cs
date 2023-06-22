using UnityEngine;

/// <summary>
/// Behavior which ensures that an object doesn't go below a minimum global Y coordinate
/// </summary>
public class DontGoBelowFloor : MonoBehaviour
{
    [SerializeField] 
    float _minY;

    void Update()
    {
        float offset = _minY - transform.position.y;
        if (offset > 0f)
        {
            // Move it back up
            transform.position = transform.position + Vector3.up * offset;
        }
    }
}

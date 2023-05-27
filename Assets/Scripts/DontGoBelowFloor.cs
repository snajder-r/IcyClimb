using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontGoBelowFloor : MonoBehaviour
{
    [SerializeField] private float minY;

    void Start()
    {
        
    }

    void Update()
    {
        float offset = minY - transform.position.y;
        if (offset > 0f)
        {
            transform.position = transform.position + Vector3.up * offset;
        }
    }
}

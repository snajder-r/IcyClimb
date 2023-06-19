using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cliff : MonoBehaviour
{
    private Collider cliffCollider;

    void Start()
    {
        cliffCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach(Component component in other.GetComponents<Component>()){
            if(component is IWallTriggerCollider)
            {
                ((IWallTriggerCollider) component).OnWallCollisionEnter(cliffCollider);
            }
        }
    }
}

public interface IWallTriggerCollider
{
    public void OnWallCollisionEnter(Collider cliff);
}

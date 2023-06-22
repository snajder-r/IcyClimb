using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cliff : MonoBehaviour
{
    private Collider _cliffCollider;

    void Start()
    {
        _cliffCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // If any trigger hits this wall, we will inform the source of the trigger that the wall was hit.
        foreach (Component component in other.GetComponents<Component>())
        {
            if (component is IWallTriggerCollider)
            {
                ((IWallTriggerCollider)component).OnWallCollisionEnter(_cliffCollider);
            }
        }
    }
}

public interface IWallTriggerCollider
{
    /// <summary>
    /// Event called by the cliff when a trigger entered the Cliff
    /// </summary>
    /// <param name="cliff">The cliff's collider</param>
    public void OnWallCollisionEnter(Collider cliff);
}

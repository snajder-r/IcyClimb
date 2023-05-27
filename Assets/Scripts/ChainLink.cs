using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChainLink : MonoBehaviour
{
    [Tooltip("If previous link is a regular Rigidbody attach it here. If it is a ChainLink it automatically registers.")]
    [SerializeField] public ChainLink previousLink;
    [SerializeField] public ChainLink nextLink;
    [SerializeField] float elasticity;

    [Header("Link replication")]
    [SerializeField] bool isLinkSpawner = false;
    [Tooltip("Distance from the next link at which we will spawn a new link. Infinity if no spawning is desired.")]
    [SerializeField] float spawnDistance = float.PositiveInfinity;
    [Tooltip("Template for new links. If none provided a copy of the next link will be created.")]
    [SerializeField] private GameObject newLinkPrefab;
    [SerializeField] private float linkSpawnCooldown = 1f;

    private Rigidbody link;
    private float currentLinkSpawnCooldown = 0f;

    void Awake()
    {
        link = GetComponent<Rigidbody>();
        if (nextLink)
        {
            // All stuff that requires this link to have a next link
            SetUpNextLink();
        }
    }

    void SetUpNextLink()
    {
        if (!newLinkPrefab && isLinkSpawner)
        {
            // We copy it in case it gets deleted!
            newLinkPrefab = Instantiate(nextLink.gameObject, transform.position, transform.rotation);
            // Important, because otherwise it will try to register itself somewhere as soon as it wakes up
            newLinkPrefab.GetComponent<ChainLink>().nextLink = null;
            newLinkPrefab.SetActive(false);
        }

        ChainLink nextLinkComponent = nextLink.GetComponent<ChainLink>();
        if (nextLinkComponent)
        {
            // If the next link is also a chainlink (and not just a simple rigidbody) let it know we latched on
            nextLinkComponent.OnLinkConnected(this);
        }
    }

    void Update()
    {
        currentLinkSpawnCooldown = Mathf.Clamp(currentLinkSpawnCooldown - Time.deltaTime, 0, 3f);
        StrainForce(previousLink);
        StrainForce(nextLink);

        if (isLinkSpawner)
        {
            SpawnNewLink();
        }
    }

    void SpawnNewLink()
    {
        if (!nextLink) return;
        if (currentLinkSpawnCooldown > 0f) return;

        Vector3 distance = nextLink.transform.position - transform.position;
        if(distance.magnitude > spawnDistance)
        {
            GameObject newLink = Instantiate(newLinkPrefab, transform.position + distance/2f, transform.rotation);
            newLink.SetActive(true);
            ChainLink newLinkComponent = newLink.GetComponent<ChainLink>();
            newLinkComponent.nextLink = nextLink;
            nextLink = newLinkComponent;
            newLinkComponent.OnLinkConnected(this);
            newLinkComponent.Awake();
        }
        currentLinkSpawnCooldown = linkSpawnCooldown;
    }

    public void OnLinkConnected(ChainLink other)
    {
        previousLink = other;
    }

    void AddForce(ChainLink caller, Vector3 force)
    {
        if (!link) return;
        link.AddForce(force * Time.deltaTime);
    }

    void StrainForce(ChainLink other)
    {
        if (!other) return;

        Vector3 force = transform.position - other.transform.position;
        force = force * (1f + elasticity);

        if (link.isKinematic)
        {
            other.AddForce(this, force*2f);
        }
        else {
            other.AddForce(this, force);
        }
    }

    private void OnTriggerEnter(Collider otherCollider)
    {
        // Ignore trigger from our neighbors
        if (nextLink) {
            if (otherCollider.gameObject == nextLink.gameObject) return;
        }
        if (previousLink)
        {
            if (otherCollider.gameObject == previousLink.gameObject) return;
        }
        

        ChainLink other = otherCollider.GetComponent<ChainLink>();
        if (!other) return;
        DestroyLoop(other);
    }


    void DestroyLoop(ChainLink other)
    {
        // Kinematic links should not be destroyed
        if (other.link.isKinematic || link.isKinematic) return;

        List<ChainLink> shortcut;
        //Forward search
        if (SearchForLink(other, true, out shortcut))
        {
            nextLink = other;
            other.OnLinkConnected(this);
            foreach (ChainLink toDelete in shortcut)
            {
                Destroy(toDelete.gameObject);
            }
        }
        //Backward search
        if (SearchForLink(other, false, out shortcut))
        {
            previousLink = other;
            other.nextLink = this;
            foreach (ChainLink toDelete in shortcut)
            {
                Destroy(toDelete.gameObject);
            }
        }
    }
    bool SearchForLink(ChainLink other, bool forward, out List<ChainLink> traversed)
    {
        ChainLink current = this;
        traversed = new List<ChainLink>();
        while (true) {
            current = forward ? current.nextLink : current.previousLink;
            if (!current)
            {
                return false;
            }
            if(current == other)
            {
                return true;
            }
            if(traversed.Contains(current))
            {
                // There shouldn't be a loop, but who knows
                return false;
            }
            if (current.link.isKinematic)
            {
                // We should not traverse kinematic links. They are fixed anchors
                return false; 
            }
            traversed.Add(current);
        }
    }
}

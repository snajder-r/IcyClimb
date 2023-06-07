using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChainLink : MonoBehaviour
{
    [field: SerializeField] public ChainLink previousLink { get; private set; }
    [field: SerializeField] public ChainLink nextLink { get; private set; }
    [SerializeField] float elasticity;

    [Header("Link replication")]
    [SerializeField] bool isSpawnerBefore = false;
    [SerializeField] bool isSpawnerAfter = false;
    [Tooltip("Distance from the next link at which we will spawn a new link. Infinity if no spawning is desired.")]
    [SerializeField] float spawnForce = float.PositiveInfinity;
    [SerializeField] float deSpawnDistance = 0f;
    [Tooltip("Template for new links. If none provided a copy of the next link will be created.")]
    [SerializeField] private GameObject newLinkPrefab;
    [SerializeField] private float linkSpawnCooldown = 1f;
    [SerializeField] private LayerMask wallLayerMask;

    private Rigidbody linkRigidBody;
    private float currentLinkSpawnCooldown = 0f;


    public static void InsertBetween(ChainLink insert, ChainLink before, ChainLink after)
    {
        insert.nextLink = after;
        insert.previousLink = before;
        after.previousLink = insert;
        before.nextLink = insert;
    }

    public bool InsertAfter(ChainLink insert)
    {
        if (!nextLink) return false;
        InsertBetween(insert, this, nextLink);
        return true;
    }

    public bool InsertBefore(ChainLink insert)
    {
        if (!previousLink) return false;
        InsertBetween(insert, previousLink, this);
        return true;
    }

    public void DisconnectSelf() => DisconnectSelf(true);
    public void DisconnectSelf(bool informNeighbors)
    {
        if (informNeighbors) { 
            if (previousLink) previousLink.nextLink = nextLink;
            if (nextLink) nextLink.previousLink = previousLink;
        }
        nextLink = null;
        previousLink = null;
    }

    public void Start()
    {
        linkRigidBody = GetComponent<Rigidbody>();

        if (nextLink && !nextLink.previousLink)
        {
            // If editor only defined the next link, let's inform our next link that we are here
            nextLink.previousLink = this;
        }
    }

    protected void Awake() => Start();

    private void Update()
    {
        if (linkRigidBody.isKinematic)
        {
            if (nextLink)
            {
                // Kinematic nodes start forward propagation
                nextLink.Forward(ComputeForce(nextLink));
            }
            if (previousLink)
            {
                // Kinematic nodes start backward propagation
                previousLink.Backward(ComputeForce(previousLink));
            }
        }

        if (!linkRigidBody.isKinematic)
        {
            // Only non-kinematic spawns can destroy itself
            DespawnIfTooCloseToNeighbors();
        }
    }

    public void Forward(Vector3 force)
    {
        if (!linkRigidBody.isKinematic)
        {
            linkRigidBody.AddForce(force);
        }


        if (isSpawnerBefore && force.magnitude > spawnForce)
        {
            SpawnBetween(previousLink, this);
        }

        // Forward propagation ends when there is no next link
        if (!nextLink) return;
        // Kinematic nodes start propagation, they don't pass it on
        if (linkRigidBody.isKinematic) return;

        nextLink.Forward(ComputeForce(nextLink));
    }

    public void Backward(Vector3 force)
    {
        linkRigidBody.AddForce(force);

        if (isSpawnerAfter && force.magnitude > spawnForce)
        {
            SpawnBetween(this, nextLink);
        }

        // Backwards propagation ends when there is no previous link
        if (!previousLink) return;
        // Kinematic nodes start backward propagation, they don't pass it on
        if (linkRigidBody.isKinematic) return;

        previousLink.Backward(ComputeForce(previousLink));
    }

    Vector3 ComputeForce(ChainLink toLink)
    {
        Vector3 force = transform.position - toLink.transform.position;
        force *= elasticity * Time.deltaTime;
        return force;
    }

    void DespawnIfTooCloseToNeighbors()
    {
        if (!nextLink || !previousLink) return;

        float distance = (previousLink.transform.position - transform.position).magnitude;
        // Don't despawn if I'm far enough away from previous node
        if (distance >= deSpawnDistance) return;

        distance = (nextLink.transform.position - transform.position).magnitude;
        // Don't despawn if I'm far enough away from next node
        if (distance >= deSpawnDistance) return;

        previousLink.nextLink = nextLink;
        nextLink.previousLink = previousLink;
        Destroy(gameObject);
    }

    void SpawnBetween(ChainLink before, ChainLink after)
    {
        currentLinkSpawnCooldown -= Time.deltaTime;

        // Stop here if we are still waiting on the cooldown
        if (currentLinkSpawnCooldown > 0f) return;

        Vector3 offset = after.transform.position - before.transform.position;

        Vector3 spawnPoint = before.transform.position + offset / 2f;

        // Make sure the new spawn point is not inside a wall
        if (!CanLinkSeeSpawnPoint(before.transform.position, spawnPoint)) return;
        if (!CanLinkSeeSpawnPoint(after.transform.position, spawnPoint)) return;

        ChainLink newLink = Instantiate(newLinkPrefab, spawnPoint, transform.rotation).GetComponent<ChainLink>();
        newLink.Start();
        InsertBetween(newLink, before, after);

        currentLinkSpawnCooldown = linkSpawnCooldown;
    }

    bool CanLinkSeeSpawnPoint(Vector3 positionA, Vector3 positionSpawn)
    {
        Ray ray = new Ray(positionA, positionSpawn - positionA);
        float rayLength = (positionSpawn - positionA).magnitude;
        return !Physics.Raycast(ray, rayLength, wallLayerMask.value);
    }



}

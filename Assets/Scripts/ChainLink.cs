using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A ChainLink is a custom hinge-like structure used to represent a rope or chain of variable length.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ChainLink : MonoBehaviour
{
    [field: Tooltip("Previous chainLink in the chain. If left null, it is set automatically be the next link, or it may remain null if this is the first link.")]
    [field: SerializeField]
    public ChainLink PreviousLink { get; private set; }

    [field: Tooltip("Next chainLink in the chain. Can be null if this is the last link")]
    [field: SerializeField]
    public ChainLink NextLink { get; private set; }

    [Tooltip("Force used to pull connected chain links towards me (both the previous and the next link).")]
    [SerializeField, FormerlySerializedAs("elasticity")]
    float _elasticity;

    [Header("Link replication")]
    [SerializeField]
    bool _isSpawnerBefore = false;
    [SerializeField]
    bool _isSpawnerAfter = false;
    [Tooltip("Distance from the next link at which we will spawn a new link. Infinity if no spawning is desired.")]
    [SerializeField]
    float _spawnDistance = float.PositiveInfinity;
    [SerializeField]
    [Tooltip("This link will despawn if the next link is closer than this")]
    float _deSpawnDistance = 0f;
    [Tooltip("Template for new links. If none provided a copy of the next link will be created.")]
    [SerializeField]
    GameObject _newLinkPrefab;
    [SerializeField]
    float _linkSpawnCooldown = 1f;
    [Tooltip("Layer mask defining what constitutes an obstruction for the rope. The rope will try not to spawn new nodes inside a wall.")]
    [SerializeField]
    LayerMask _wallLayerMask;

    Rigidbody _linkRigidBody;

    float _currentLinkSpawnCooldown = 0f;


    public void Start()
    {
        _linkRigidBody = GetComponent<Rigidbody>();

        if (NextLink && !NextLink.PreviousLink)
        {
            // If editor only defined the next link, let's inform our next link that we are here
            NextLink.PreviousLink = this;
        }
    }

    // Probably unnecessary, but at some point I think this solved some problem. Scared to remove it now.
    protected void Awake() => Start();

    private void FixedUpdate()
    {
        if (_linkRigidBody.isKinematic)
        {
            if (NextLink)
            {
                // Kinematic nodes start forward propagation of force
                NextLink.Forward(ComputeForce(NextLink));
            }
            if (PreviousLink)
            {
                // Kinematic nodes start backward propagation of force
                PreviousLink.Backward(ComputeForce(PreviousLink));
            }
        }

        if (!_linkRigidBody.isKinematic)
        {
            // Only non-kinematic spawns can destroy itself
            DespawnIfTooCloseToNeighbors();
        }
    }

    /// <summary>
    /// Insert a new chain link between two existing links
    /// </summary>
    /// <param name="insert"> The link to insert </param>
    /// <param name="before"> The link after which we want to insert </param>
    /// <param name="after"> The link before which we want to insert </param>
    public static void InsertBetween(ChainLink insert, ChainLink before, ChainLink after)
    {
        insert.NextLink = after;
        insert.PreviousLink = before;
        after.PreviousLink = insert;
        before.NextLink = insert;
    }

    /// <summary>
    /// Insert a new link after this link. Will fail if this is the last link, 
    /// as the current implementation doesn't support insertion of dangling links
    /// </summary>
    /// <param name="insert"> The link to insert </param>
    /// <returns> Whether insertion was successful </returns>
    public bool InsertAfter(ChainLink insert)
    {
        if (!NextLink) return false;
        InsertBetween(insert, this, NextLink);
        return true;
    }

    /// <summary>
    /// Insert a new link before this link. Will fail if this is the first link, 
    /// as the current implementation doesn't support insertion of dangling links
    /// </summary>
    /// <param name="insert"> The link to insert </param>
    /// <returns> Whether insertion was successful </returns>
    public bool InsertBefore(ChainLink insert)
    {
        if (!PreviousLink) return false;
        InsertBetween(insert, PreviousLink, this);
        return true;
    }

    /// <summary>
    /// Disconnect self from the chain and reconnect the two neighboring links to each other
    /// </summary>
    public void DisconnectSelf() => DisconnectSelf(true);

    /// <summary>
    /// Disconnect self from the chain
    /// </summary>
    /// <param name="informNeighbors"> Whether neighboring links should be connected to each other to close the gap </param>
    public void DisconnectSelf(bool informNeighbors)
    {
        if (informNeighbors)
        {
            if (PreviousLink) PreviousLink.NextLink = NextLink;
            if (NextLink) NextLink.PreviousLink = PreviousLink;
        }
        NextLink = null;
        PreviousLink = null;
    }


    /// <summary>
    /// Forward processing of chain physics.
    /// Applies force from previous link, decides whether to spawn a new previous link, and then call forward on the next link.
    /// </summary>
    void Forward(Vector3 force)
    {
        if (!_linkRigidBody.isKinematic)
        {
            _linkRigidBody.AddForce(force);
        }

        // If we are a link spawner, check if we should now spawn a new link before ourselves.
        // Link spawning is organized in the forward/backward methods to make sure it happens in order and we don't accidentally
        // spawn a link twice (once before and once after another link)
        if (_isSpawnerBefore && force.magnitude > _spawnDistance)
        {
            SpawnBetween(PreviousLink, this);
        }

        // Forward propagation ends when there is no next link
        if (!NextLink) return;
        // Kinematic nodes start propagation, they don't pass it on
        if (_linkRigidBody.isKinematic) return;

        NextLink.Forward(ComputeForce(NextLink));
    }

    /// <summary>
    /// Backward processing of chain physics.
    /// Applies force from next link, decides whether to spawn a new next link, and then call backward on the previous link.
    /// </summary>
    void Backward(Vector3 force)
    {
        _linkRigidBody.AddForce(force);


        // If we are a link spawner, check if we should now spawn a new link before ourselves.
        // Link spawning is organized in the forward/backward methods to make sure it happens in order and we don't accidentally
        // spawn a link twice (once before and once after another link)
        if (_isSpawnerAfter && force.magnitude > _spawnDistance)
        {
            SpawnBetween(this, NextLink);
        }

        // Backwards propagation ends when there is no previous link
        if (!PreviousLink) return;
        // Kinematic nodes start backward propagation, they don't pass it on
        if (_linkRigidBody.isKinematic) return;

        PreviousLink.Backward(ComputeForce(PreviousLink));
    }

    /// <summary>
    /// Computes the force which pulls another link towards us.
    /// </summary>
    /// <param name="toLink">Which link to compute for</param>
    /// <returns>The computed force as a vector</returns>
    Vector3 ComputeForce(ChainLink toLink)
    {
        Vector3 force = transform.position - toLink.transform.position;
        force *= _elasticity;
        return force;
    }

    void DespawnIfTooCloseToNeighbors()
    {
        if (!NextLink || !PreviousLink) return;

        float distance = (PreviousLink.transform.position - transform.position).magnitude;
        // Don't despawn if I'm far enough away from previous node
        if (distance >= _deSpawnDistance) return;

        distance = (NextLink.transform.position - transform.position).magnitude;
        // Don't despawn if I'm far enough away from next node
        if (distance >= _deSpawnDistance) return;

        PreviousLink.NextLink = NextLink;
        NextLink.PreviousLink = PreviousLink;
        Destroy(gameObject);
    }

    void SpawnBetween(ChainLink before, ChainLink after)
    {
        _currentLinkSpawnCooldown -= Time.deltaTime;

        // Stop here if we are still waiting on the cooldown
        if (_currentLinkSpawnCooldown > 0f) return;

        Vector3 offset = after.transform.position - before.transform.position;

        Vector3 spawnPoint = before.transform.position + offset / 2f;

        // Make sure the new spawn point is not inside a wall
        if (!CanLinkSeeSpawnPoint(before.transform.position, spawnPoint)) return;
        if (!CanLinkSeeSpawnPoint(after.transform.position, spawnPoint)) return;

        ChainLink newLink = Instantiate(_newLinkPrefab, spawnPoint, transform.rotation).GetComponent<ChainLink>();
        newLink.Start();
        InsertBetween(newLink, before, after);

        _currentLinkSpawnCooldown = _linkSpawnCooldown;
    }

    bool CanLinkSeeSpawnPoint(Vector3 positionA, Vector3 positionSpawn)
    {
        Ray ray = new(positionA, positionSpawn - positionA);
        float rayLength = (positionSpawn - positionA).magnitude;
        return !Physics.Raycast(ray, rayLength, _wallLayerMask.value);
    }

}

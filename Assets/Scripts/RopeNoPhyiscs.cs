using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeNoPhyiscs : MonoBehaviour
{

    [SerializeField] private Transform ropeIn;
    [SerializeField] private Transform ropeOut;

    public static RopeNoPhyiscs instance;

    private TrailRenderer trail;

    private int layerMaskWall;

    void Start()
    {
        // Singleton pattern ...  sort of (doesn't assure that there is only one instance)
        instance = this;
        trail = GetComponentInChildren<TrailRenderer>();
        layerMaskWall = LayerMask.GetMask(new string[] { "Wall" });

        trail.AddPosition(transform.position);
        trail.AddPosition(ropeIn.position);
        trail.AddPosition(ropeOut.position);
        trail.AddPosition(transform.position);
    }


    void Update()
    {
        RebuildRope();
    }
    void RebuildRope() {
        List<Vector3> positions = new List<Vector3>();

        AddRopeFeed(positions);

        AddAnchorPositions(positions);

        positions.Add(ropeOut.position);
        positions.Add(transform.position);



        AddSlackPositions(positions);
    }

    void AddAnchorPositions(List<Vector3> positions)
    {

    }

    void AddRopeFeed(List<Vector3> positions)
    {
        
    }
    

    void AddSlackPositions(List<Vector3> positions)
    {
        for(int i = 1; i < positions.Count; i++)
        {
            RaycastHit hit;
            Physics.Raycast(positions[i - 1], positions[i] - positions[i - 1]);
        }
    }
}

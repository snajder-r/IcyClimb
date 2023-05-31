using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallDummy : MonoBehaviour
{
    [SerializeField] private Transform anchor;
    [SerializeField] private Transform dummy;
    [SerializeField] private ConfigurableJoint joint;

    private int movementBufferIndex = 0;
    private float[] movementBuffer = new float[10];
    private Vector3 lastDummyPosition;

    public Vector3 DummyPosition { get { return dummy.position; } }

    public float MaxFallDistance { get { return joint.linearLimit.limit; } }

    public bool IsFalling { get { return gameObject.activeInHierarchy; } }

    public float WindowedMeanMovement
    {
        get
        {
            float s = 0f;
            foreach (float x in movementBuffer)
            {
                s += x;
            }
            return s / movementBuffer.Length;
        }
    }

    void Start()
    {
        DummyCollider collider = (DummyCollider) dummy.gameObject.AddComponent(typeof(DummyCollider));
        collider.parentDummy = this;

        gameObject.SetActive(false);
    }

    private void Update()
    {

    }
    
    private void SampleMovement()
    {
        movementBuffer[movementBufferIndex] = (dummy.position - lastDummyPosition).magnitude;

        movementBufferIndex = ++movementBufferIndex % movementBuffer.Length;
    }

    public void StartFalling(Vector3 playerPosition, Quaternion playerRotation, Vector3 anchorPosition)
    {
        anchor.transform.position = anchorPosition;
        dummy.transform.position = playerPosition;
        dummy.transform.rotation = playerRotation;

        SoftJointLimit limit = joint.linearLimit;
        limit.limit = (anchorPosition - playerPosition).magnitude;
        joint.linearLimit = limit;

        gameObject.SetActive(true);
    }

    public void StartFalling(Vector3 playerPosition, Quaternion playerRotation)
    {
        StartFalling(playerPosition, playerRotation, playerPosition + Vector3.up*-500);
    }

    public void StopFalling()
    {
        gameObject.SetActive(false);
    }

    public Vector3 GetMotion(Vector3 playerPosition)
    {
        // Record the dummy's own movement
        SampleMovement();

        return dummy.position - playerPosition;
    }

    public Quaternion GetRotation()
    {
        return dummy.rotation;
    }
}


class DummyCollider : MonoBehaviour
{
    public FallDummy parentDummy;
    private void Update()
    {
        if (transform.position.y < 0f)
        {
            parentDummy.StopFalling();
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        collision.GetContacts(GameManager.ContactPointBuffer);
        Quaternion inverseMyRotation = Quaternion.Inverse(transform.rotation);
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = GameManager.ContactPointBuffer[i];
            Vector3 normal = inverseMyRotation * contact.normal;
            float verticalAngle = Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up));
            Debug.Log(verticalAngle);
            if (verticalAngle > 0.9f)
            {
                // Looks like we hit the floor
                parentDummy.StopFalling();
                return;
            }
        }
    }
}
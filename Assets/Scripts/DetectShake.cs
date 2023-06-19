using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DetectShake : MonoBehaviour
{
    [SerializeField] int numberOfSamples;
    [SerializeField] float differenceThreshold;
    [SerializeField] float minimumMovement;

    [SerializeField] UnityEvent onShookEntered;
    [SerializeField] UnityEvent onShaking;
    [SerializeField] UnityEvent onShookExited;
    [SerializeField] bool baseOnLocalPosition;

    SmoothedVector3 change;
    Vector3 lastPosition;

    bool shaking;

    Vector3 position => baseOnLocalPosition ? transform.localPosition : transform.position;

    // Start is called before the first frame update
    void Start()
    {
        change = new SmoothedVector3(numberOfSamples, Vector3.zero);
        lastPosition = position;
        shaking = false;
    }

    // Update is called once per frame
    void Update()
    {
        change.Add((position - lastPosition) / Time.deltaTime);
        lastPosition = position;

        Vector3[] twoParts = change.BinnedMeans(2);
        if (twoParts[0].magnitude < minimumMovement || twoParts[1].magnitude < minimumMovement)
        {
            if (shaking)
            {
                // We were shaking but stopped moving
                onShookExited.Invoke();
                shaking = false;
            }
            return;
        }

        Vector3 partDifference = twoParts[0] - twoParts[1];
        if (partDifference.magnitude > differenceThreshold)
        {
            if (!shaking)
            {
                // We started shaking
                onShookEntered.Invoke();
                shaking = true;
                return;
            }
        }

        if (shaking)
        {
            // We were shaking before and we are still shaking
            //onShaking.Invoke();
        }
    }
}

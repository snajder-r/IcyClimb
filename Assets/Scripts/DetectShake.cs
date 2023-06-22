using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Behavior to detect whether an object is shook.
/// </summary>
public class DetectShake : MonoBehaviour
{
    [Tooltip("Number of frames to smooth movement over")]
    [SerializeField] 
    int _numberOfSamples;
    [Tooltip("Minimum speed change (delta speed) to consider it as shaking")]
    [SerializeField] 
    float _differenceThreshold;
    [Tooltip("Minimum speed to consider the object as moving. If the object moved and then slowed down below this threshold, it does not count as shaking, even if the speed difference threshold passes.")]
    [SerializeField] 
    float _minimumMovement;
    [Tooltip("If true, speed is calculated based on local position. If false it is calculated based on global position")]
    [SerializeField]
    bool _baseOnLocalPosition;

    [SerializeField] 
    UnityEvent _onShookEntered;
    [SerializeField] 
    UnityEvent _onShookExited;

    SmoothedVector3 _change;
    Vector3 _lastPosition;

    bool _isShaking;

    public Vector3 Position { get => _baseOnLocalPosition ? transform.localPosition : transform.position; }

    void Start()
    {
        _change = new SmoothedVector3(_numberOfSamples, Vector3.zero);
        _lastPosition = Position;
        _isShaking = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Sample the current speed
        _change.Add((Position - _lastPosition) / Time.deltaTime);
        _lastPosition = Position;

        // Bin the speed samples into two the first half and second half,
        // compute means from both
        Vector3[] twoParts = _change.BinnedMeans(2);
        if (twoParts[0].magnitude < _minimumMovement || twoParts[1].magnitude < _minimumMovement)
        {
            // We either weren't moving before or we aren't moving now
            if (_isShaking)
            {
                // We were shaking but stopped moving
                _onShookExited.Invoke();
                _isShaking = false;
            }
            return;
        }

        // Compute speed difference from first mean and second mean
        Vector3 partDifference = twoParts[0] - twoParts[1];
        if (partDifference.magnitude > _differenceThreshold)
        {
            // The difference is sufficient and we were always moving
            if (!_isShaking)
            {
                // We started shaking
                _onShookEntered.Invoke();
                _isShaking = true;
                return;
            }
        }
    }
}

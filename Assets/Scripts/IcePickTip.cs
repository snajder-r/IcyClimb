using UnityEngine;
using UnityEngine.Serialization;

public class IcePickTip : MonoBehaviour, IWallTriggerCollider
{
    [Tooltip("Sound effects randomly played when lodged")]
    [SerializeField]
    AudioClip[] _penetrateIceSounds;
    [Tooltip("Sound effects randomly played when dislodged")]
    [SerializeField]
    AudioClip[] _dislodgeSound;
    [Tooltip("Minimum point velocity of the tip in order to lodge the ice pick")]
    [SerializeField]
    float _minimumIcePenetrationVelocity = 0f;
    [Tooltip("Maximum angle between the tip forward and the wall normal")]
    [SerializeField]
    float _maxAngleBetweenTipAndWall = 45f;

    [Tooltip("Number of frames over which the tip velocity is smoothed")]
    [SerializeField]
    int _tipVelocitySmoothing = 0;

    [Tooltip("Layers which can be penetrated by the ice pick")]
    [SerializeField]
    LayerMask _wallLayerMask;

    private IcePick _icePick;
    private AudioSource _audioSource;
    /// <summary>
    /// A particle effect played when the ice is hit
    /// </summary>
    private ParticleSystem _penetrateIceEffect;
    /// <summary>
    /// Stores a smoothed representation of the ice pick tip's point velocity
    /// </summary>
    private SmoothedVector3 _tipVelocity;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _penetrateIceEffect = GetComponent<ParticleSystem>();
        _icePick = GetComponentInParent<IcePick>();
        _tipVelocity = new SmoothedVector3(_tipVelocitySmoothing);
    }

    private void Update()
    {
        // Sample point velocity in a smoothed vector so we can later query it
        Vector3 velocity = _icePick.LodgeAbleRigidbody.GetPointVelocity(transform.position);
        _tipVelocity.Add(velocity);
    }

    public void OnDislodge()
    {
        // Play dislodge sound
        int soundIndex = Random.Range(0, _dislodgeSound.Length);
        _audioSource.PlayOneShot(_dislodgeSound[soundIndex], 3f);
    }

    /// <summary>
    /// Called by the wall when the trigger in the ice pick tip hit a wall. 
    /// Decides whether speed and angle are sufficient to allow lodging the ice pick in the wall.
    /// If so, it will play the appropriate effects and inform the icepick that it is now lodged.
    /// </summary>
    /// <param name="cliff">The collider of the wall we hit</param>
    public void OnWallCollisionEnter(Collider cliff)
    {
        // Smoothed tip point velocity
        Vector3 velocity = _tipVelocity.Mean;

        // Are we hitting towards the wall (against its normal)?
        // The angle between the wall normal and the ice pick tip forward
        float angleToWall = -1f;

        // Since the wall collider is not necessarily convex, we can't find the wall normal through functions such as
        // "ClosestPointOnSurface" which don't work on MeshCollider.
        // Therefore, we perform a raycast down the tip and see if we hit a wall. 
        // When we do, we can determine the normal of the cliff on that hit.
        if (Physics.Raycast(transform.position - transform.forward, transform.forward, out RaycastHit hit, 1.5f, _wallLayerMask))
        {
            // Yes, we hit a wall and can now determine the angle of our hit
            angleToWall = Vector3.Dot(transform.forward, -hit.normal);
        }
        // Convert the angle to degrees
        angleToWall = Mathf.Rad2Deg * Mathf.Acos(angleToWall);

        if (angleToWall > _maxAngleBetweenTipAndWall)
        {
            // The hit was not at a proper angle
            return;
        }

        // Weigh velocity based on its angle between it and the ice pick tip
        float speed = Vector3.Dot(transform.forward, velocity);
        if (speed < _minimumIcePenetrationVelocity)
        {
            // The speed was not sufficient
            return;
        }

        // If we reach here, all validations passed and we can lodge the ice pick
        PlayPenetrateIceSound(speed);
        PlayPenetrateIceEffect();
        _icePick.Lodge();
    }

    /// <summary>
    /// Plays the sound effect of hitting the ice. The volume is determined based on the velocity.
    /// </summary>
    /// <param name="velocity">The velocity of the hit</param>
    void PlayPenetrateIceSound(float velocity)
    {
        float volume = 0.25f + Mathf.Lerp(_minimumIcePenetrationVelocity, _minimumIcePenetrationVelocity * 3, velocity);
        int soundIndex = Random.Range(0, _penetrateIceSounds.Length);
        _audioSource.PlayOneShot(_penetrateIceSounds[soundIndex], volume);
    }

    /// <summary>
    /// Play the particle effect when the ice is hit
    /// </summary>
    void PlayPenetrateIceEffect()
    {
        _penetrateIceEffect.Play();
    }
}

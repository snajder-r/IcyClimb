using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Purpose of this behavior was to sort of remove locomotion-unrelated things from the player locomotion.
/// In the end, not much was left, so this mostly just plays sounds
/// </summary>
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    public Transform PlayerCenterOfGravity;

    [SerializeField] private PlayerLocomotion _locomotion;
    [SerializeField, Tooltip("Sound played when losing your footing and starting to fall")] private AudioClip[] _slippingSound;
    [SerializeField] private AudioClip[] _landingSound;
    [SerializeField] private AudioClip[] _footstepSound;

    [Tooltip("This is where sounds like falling sounds will play")]
    [SerializeField] private AudioSource _feetAudio;

    private Vector3 _lastPosition;
    private float _cumulativeMovement;

    private Cooldown _slippingSoundCooldown;
    private Cooldown _landingSoundCooldown;
    void Awake()
    {
        Instance = this;
        _lastPosition = transform.position;

        _slippingSoundCooldown = new Cooldown(5f);
        _landingSoundCooldown = new Cooldown(5f);
    }

    void Update()
    {
        PlayFootsteps();
    }

    public void PlaySlippingSound() => PlayRandomSound(_slippingSound, _slippingSoundCooldown);
    public void PlayLandingSound() => PlayRandomSound(_landingSound, _landingSoundCooldown);
    void PlayFootsteps()
    {
        if (_locomotion.IsGrounded)
        {
            // Play a footstep sound whenever we walked about 0.75 meters
            _cumulativeMovement += (transform.position - _lastPosition).magnitude;
            if (_cumulativeMovement > 0.75f)
            {
                PlayRandomSound(_footstepSound);
                _cumulativeMovement = 0f;
            }
        }
        _lastPosition = transform.position;
    }
    void PlayRandomSound(AudioClip[] sounds) => PlayRandomSound(sounds, null);
    void PlayRandomSound(AudioClip[] sounds, Cooldown cooldown)
    {
        if (cooldown is not null)
        {
            if (!cooldown.Acquire()) return;
        }
        int index = Random.Range(0, sounds.Length);
        _feetAudio.PlayOneShot(sounds[index]);
    }
}

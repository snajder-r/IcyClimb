using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// When falling fast (or moving fast for any other reason), play visual and audio effects that get stronger with the speed
/// </summary>
public class SpeedBasedScreenDistortion : MonoBehaviour
{
    [SerializeField, Tooltip("Transform used to determine movement of the player")] 
    Transform _player;
    [SerializeField, Tooltip("Post processing volume should contain chromatic abberation, lens distortion, and motion blur")] 
    Volume _postProcessingVolume;
    [SerializeField, Tooltip("Minimum speed (m/s) at which speed effects start to play")] 
    float _minSpeed;
    [SerializeField, Tooltip("Maximum speed (m/s) at which the effects play at highest intensity")] 
    float _maxSpeed;
    [SerializeField] 
    AudioSource _audioSource;
    [SerializeField] 
    AudioClip _fallingWindSound;
    [SerializeField, Tooltip("Audio volume multiplier for the speed sound")] 
    float _volumeMultiplier = 0.5f;
    [SerializeField, Tooltip("Speed will be computed from collecting this many samples of delta-position and then calculating the average")] 
    int _numberFramesForSmoothing;
    [SerializeField, Tooltip("The number of frames in which we have to move beyond the minimum speed before activating effects. This is meant to catch some single-(or few-)frame speedbumps (e.g. caused by framerate drops)")] 
    int _minFramesToActivation;

    /// <summary>
    /// The smoothed delta position (speed)
    /// </summary>
    SmoothedVector3 _smoothedChange;

    /// <summary>
    /// Current velocity computed from the smoothedChange
    /// </summary>
    float _velocity;

    /// <summary>
    /// Effect intensity calculated from the velocity
    /// </summary>
    float _intensity;

    /// <summary>
    /// The last position for computing speed
    /// </summary>
    Vector3 _lastPosition;
    /// <summary>
    /// How many frames we have been beyond the minimum speed
    /// </summary>
    int _numFramesActivated;

    ChromaticAberration _chromaticAberration;
    LensDistortion _lensDistortion;
    MotionBlur _motionBlur;

    /// <summary>
    /// Used for lensDistortion which bounces up and down. While moving at heigh speed, the distortion will oscillate.
    /// If it is positive, lens distortion increases, and if it is negative it decreases.
    /// </summary>
    float intensityChangeSign = 1f;
    
    void Start()
    {
        _postProcessingVolume.profile.TryGet<ChromaticAberration>(out _chromaticAberration);
        _postProcessingVolume.profile.TryGet<LensDistortion>(out _lensDistortion);
        _postProcessingVolume.profile.TryGet<MotionBlur>(out _motionBlur);

        // I found that using medianmagnitude for smoothing works much better on Android, as it catches outliers.
        // I'm still not sure why framedrops cause such outliers in the first place (it should be caught by dividing by deltatime)
        // but this workaround works, so I didn't investigate further.
        _smoothedChange = new SmoothedVector3(_numberFramesForSmoothing, Vector3.zero, SmoothedVector3.Mode.MedianMagnitude);
        _lastPosition = _player.position;
    }

    void LateUpdate()
    {
        // Sample current speed
        _smoothedChange.Add((_player.position - _lastPosition) / Time.deltaTime);
        // Compute the mean
        _velocity = _smoothedChange.Mean.magnitude;

        if (_velocity > _minSpeed) {
            // We are above minimum speed
            _numFramesActivated += 1;
            if(_numFramesActivated > _minFramesToActivation)
            {
                // We have been above minimum speed for sufficiently many frames
                _intensity = Mathf.InverseLerp(_minSpeed, _maxSpeed, _velocity);
            }
            else
            {
                // We still need to be above minimum speed before we activate effects
                _intensity = 0f;
            }
        }
        else
        {
            // We are below minimum speed
            _numFramesActivated = 0;
            _intensity = 0f;
        }

        if (_intensity > 0)
        {
            ApplyVisualEffects();
            PlayFallingSound();
        }
        else
        {
            EndVisualEffects();
            EndFallingSound();
        }

        _lastPosition = _player.position;
    }

    void ApplyVisualEffects()
    {
        _chromaticAberration.active = true;
        _chromaticAberration.intensity.value = _intensity;

        _motionBlur.active = true;
        _motionBlur.intensity.value = _intensity;

        _lensDistortion.active = true;

        // Oscillating screen distortion
        float deltaDistortion = intensityChangeSign * Random.Range(0f, 0.1f) * _intensity;
        _lensDistortion.intensity.value += deltaDistortion;
        if (Mathf.Abs(_lensDistortion.intensity.value) > 0.25f)
        {
            // Change direction
            intensityChangeSign *= -1f;
            _lensDistortion.intensity.value = Mathf.Clamp(_lensDistortion.intensity.value, -0.25f, 0.25f);
        }
    }

    void PlayFallingSound()
    {
        if(!_audioSource.isPlaying)
        {
            _audioSource.clip = _fallingWindSound;
            _audioSource.loop = true;
            _audioSource.Play();
            _audioSource.time = Random.Range(0f, _fallingWindSound.length);
        }

        _audioSource.volume = _intensity * _volumeMultiplier;
    }

    void EndVisualEffects()
    {
        _lensDistortion.active = false;
        _chromaticAberration.active = false;
        _motionBlur.active = false;
    }

    void EndFallingSound()
    {
        _audioSource.Stop();
        _audioSource.clip = null;
    }
}

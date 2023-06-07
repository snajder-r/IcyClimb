using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeedBasedScreenDistortion : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Volume postProcessingVolume;
    [SerializeField] float minSpeed;
    [SerializeField] float maxSpeed;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip fallingWindSound;
    [SerializeField] float volumeMultiplier = 0.5f;

    [ShowOnly] [SerializeField] float velocity;
    [ShowOnly] [SerializeField] float intensity;

    float lastDeltaTime;
    private Vector3 lastPosition;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private MotionBlur motionBlur;
    [ShowOnly]
    [SerializeField]
    private float intensityChangeSign = 1f;
    
    void Start()
    {
        postProcessingVolume.profile.TryGet<ChromaticAberration>(out chromaticAberration);
        postProcessingVolume.profile.TryGet<LensDistortion>(out lensDistortion);
        postProcessingVolume.profile.TryGet<MotionBlur>(out motionBlur);
        lastPosition = player.position;
        lastDeltaTime = Time.deltaTime;
    }

    
    void Update()
    {
        velocity = (player.position - lastPosition).magnitude / lastDeltaTime;

        if (velocity > minSpeed) { 
            intensity = Mathf.InverseLerp(minSpeed, maxSpeed, velocity);
        }
        else
        {
            intensity = 0f;
        }

        if (intensity > 0)
        {
            ApplyVisualEffects();
            PlayFallingSound();
        }
        else
        {
            EndVisualEffects();
            EndFallingSound();
        }

        lastDeltaTime = Time.deltaTime;
        lastPosition = player.position;
    }

    void ApplyVisualEffects()
    {
        chromaticAberration.active = true;
        chromaticAberration.intensity.value = intensity;

        motionBlur.active = true;
        motionBlur.intensity.value = intensity;

        lensDistortion.active = true;
        float deltaDistortion = intensityChangeSign * Random.Range(0f, 0.1f) * intensity;
        lensDistortion.intensity.value += deltaDistortion;
        if (Mathf.Abs(lensDistortion.intensity.value) > 0.25f)
        {
            intensityChangeSign *= -1f;
            lensDistortion.intensity.value = Mathf.Clamp(lensDistortion.intensity.value, -0.25f, 0.25f);
        }
    }

    void PlayFallingSound()
    {
        if(!audioSource.isPlaying)
        {
            audioSource.clip = fallingWindSound;
            audioSource.loop = true;
            audioSource.Play();
            audioSource.time = Random.Range(0f, fallingWindSound.length);
        }

        audioSource.volume = intensity * volumeMultiplier;
    }

    void EndVisualEffects()
    {
        lensDistortion.active = false;
        chromaticAberration.active = false;
        motionBlur.active = false;
    }

    void EndFallingSound()
    {
        audioSource.Stop();
        audioSource.clip = null;
    }
}

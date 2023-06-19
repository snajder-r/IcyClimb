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
    [SerializeField] int numberFramesForSmoothing;
    [SerializeField] int minFramesToActivation;

    [ShowOnly] [SerializeField] SmoothedVector3 smoothedChange;
    [ShowOnly] [SerializeField] float velocity;
    [ShowOnly] [SerializeField] float intensity;
    

    private Vector3 lastPosition;
    private int numFramesActivated;
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
        smoothedChange = new SmoothedVector3(numberFramesForSmoothing, Vector3.zero, MODE.MedianMagnitude);
        lastPosition = player.position;
    }

    
    void LateUpdate()
    {
        smoothedChange.Add((player.position - lastPosition) / Time.deltaTime);
        velocity = smoothedChange.Mean.magnitude;

        if (velocity > minSpeed) {
            numFramesActivated += 1;
            if(numFramesActivated > minFramesToActivation)
            {
                intensity = Mathf.InverseLerp(minSpeed, maxSpeed, velocity);
            }
            else
            {
                intensity = 0f;
            }
        }
        else
        {
            numFramesActivated = 0;
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

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
    
    [ShowOnly] [SerializeField] float velocity;
    [ShowOnly] [SerializeField] float intensity;

    float lastDeltaTime;
    private Vector3 lastPosition;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    [ShowOnly]
    [SerializeField]
    private float intensityChangeSign = 1f;
    
    void Start()
    {
        postProcessingVolume.profile.TryGet<ChromaticAberration>(out chromaticAberration);
        postProcessingVolume.profile.TryGet<LensDistortion>(out lensDistortion);
        lastPosition = player.position;
        lastDeltaTime = Time.deltaTime;
    }

    
    void Update()
    {
        velocity = (player.position - lastPosition).magnitude / lastDeltaTime;


        if (velocity > minSpeed) { 
            intensity = Mathf.Lerp(minSpeed, maxSpeed, velocity);
        }
        else
        {
            intensity = 0f;
        }

        if (intensity > 0)
        {
            chromaticAberration.active = true;
            chromaticAberration.intensity.value = intensity;

            lensDistortion.active = true;
            float deltaDistortion = intensityChangeSign * Random.Range(0f, 0.1f) * intensity;
            lensDistortion.intensity.value += deltaDistortion;
            if(Mathf.Abs(lensDistortion.intensity.value) > 0.25f)
            {
                intensityChangeSign *= -1f;
                lensDistortion.intensity.value = Mathf.Clamp(lensDistortion.intensity.value, -0.25f, 0.25f);
            }

        }
        else
        {
            lensDistortion.active = false;
            chromaticAberration.active = false;
        }

        lastDeltaTime = Time.deltaTime;
        lastPosition = player.position;



    }
}

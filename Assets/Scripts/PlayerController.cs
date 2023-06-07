using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [SerializeField] public Transform playerCenterOfGravity;
    [SerializeField] private PlayerLocomotion locomotion;
    [SerializeField] private AudioClip[] slippingSound;
    [SerializeField] private AudioClip[] landingSound;
    [SerializeField] private AudioClip[] footstepSound;


    [Tooltip("This is where sounds like falling sounds will play")]
    [SerializeField] private AudioSource feetAudio;

    private Vector3 lastPosition;
    private float cumulativeMovement;

    private Cooldown slippingSoundCooldown;
    private Cooldown landingSoundCooldown;

    void Awake()
    {
        instance = this;
        lastPosition = transform.position;

        slippingSoundCooldown = new Cooldown(5f);
        landingSoundCooldown = new Cooldown(5f);

    }

    // Update is called once per frame
    void Update()
    {
        PlayFootsteps();
    }

    void PlayFootsteps()
    {
        if (locomotion.IsGrounded)
        {
            cumulativeMovement += (transform.position - lastPosition).magnitude;
            if (cumulativeMovement > 0.75f)
            {
                PlayRandomSound(footstepSound);
                cumulativeMovement = 0f;
            }
        }
        lastPosition = transform.position;
    }


    public void PlaySlippingSound() => PlayRandomSound(slippingSound, slippingSoundCooldown);
    public void PlayLandingSound() => PlayRandomSound(landingSound, landingSoundCooldown);
    void PlayRandomSound(AudioClip[] sounds) => PlayRandomSound(sounds, null);
    void PlayRandomSound(AudioClip[] sounds, Cooldown cooldown)
    {
        if (cooldown is not null)
        {
            if (!cooldown.Acquire()) return;
        }
        int index = Random.Range(0, sounds.Length);
        feetAudio.PlayOneShot(sounds[index]);
    }
}

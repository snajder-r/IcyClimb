using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class IcePickTip : MonoBehaviour
{
    [SerializeField] AudioClip[] penetrateIceSounds;
    [SerializeField] AudioClip[] dislodgeSound;
    [SerializeField] float minimumIcePenetrationVelocity = 0f;

    private IcePick icePick;

    private AudioSource audioSource;
    private ParticleSystem penetrateIceEffect;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        penetrateIceEffect = GetComponent<ParticleSystem>();
        icePick = GetComponentInParent<IcePick>();
    }
    
    void PlayPenetrateIceSound(float velocity)
    {
        float volume = 0.25f + Mathf.Lerp(minimumIcePenetrationVelocity, minimumIcePenetrationVelocity * 3, velocity);
        int soundIndex = Random.Range(0, penetrateIceSounds.Length);
        audioSource.PlayOneShot(penetrateIceSounds[soundIndex], volume);
    }

    public void OnDislodge()
    {
        int soundIndex = Random.Range(0, dislodgeSound.Length);
        audioSource.PlayOneShot(dislodgeSound[soundIndex], 3f);
    }

    void PlayPenetrateIceEffect()
    {
        penetrateIceEffect.Play();
    }

    public void OnWallCollisionEnter() {
        Vector3 velocity = icePick.rigidBody.GetPointVelocity(transform.position);

        float angle = Vector3.Dot(transform.forward, velocity.normalized);
        float speed = velocity.magnitude * angle;

        if (speed < minimumIcePenetrationVelocity)
        {
            return;
        }

        PlayPenetrateIceSound(speed);
        PlayPenetrateIceEffect();
        icePick.Lodge();
    }

}

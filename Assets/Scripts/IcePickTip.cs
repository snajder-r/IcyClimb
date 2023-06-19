using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class IcePickTip : MonoBehaviour, IWallTriggerCollider
{
    [SerializeField] AudioClip[] penetrateIceSounds;
    [SerializeField] AudioClip[] dislodgeSound;
    [SerializeField] float minimumIcePenetrationVelocity = 0f;
    [SerializeField] float maxAngleBetweenTipAndWall = 45f;
    [Tooltip("Number of frames over which the tip velocity is smoothed")]
    [SerializeField] int TipVelocitySmoothing = 0;

    private IcePick icePick;

    private AudioSource audioSource;
    private ParticleSystem penetrateIceEffect;
    private SmoothedVector3 tipVelocity;
    private int iceLayerMask;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        penetrateIceEffect = GetComponent<ParticleSystem>();
        icePick = GetComponentInParent<IcePick>();
        tipVelocity = new SmoothedVector3(TipVelocitySmoothing);
        iceLayerMask = ~LayerMask.GetMask(new string[] { "Wall" });
    }

    private void Update()
    {
        Vector3 velocity = icePick.rigidBody.GetPointVelocity(transform.position);
        tipVelocity.Add(velocity);
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

    
    
    public void OnWallCollisionEnter(Collider cliff) {
        Vector3 velocity = tipVelocity.Mean;
        
        // Are we hitting towards the wall (against its normal)?
        RaycastHit hit;
        float angleToWall = -1f;
        
        if(Physics.Raycast(transform.position - transform.forward, transform.forward, out hit, 1.5f, iceLayerMask))
        {
            angleToWall = Vector3.Dot(transform.forward, -hit.normal);           
        }
        angleToWall = Mathf.Rad2Deg * Mathf.Acos(angleToWall);
        if(angleToWall > maxAngleBetweenTipAndWall)
        {
            return;
        }

        // Weigh velocity based on its angle between it and the ice pick tip
        float speed = Vector3.Dot(transform.forward, velocity);

        if (speed < minimumIcePenetrationVelocity)
        {
            return;
        }

        PlayPenetrateIceSound(speed);
        PlayPenetrateIceEffect();
        icePick.Lodge();
    }

}

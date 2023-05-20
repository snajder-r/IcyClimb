using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePick : MonoBehaviour
{
    [SerializeField] AudioClip[] penetrateIceSounds;

    private AudioSource m_AudioSource;
    private Rigidbody m_RigidBody;

    // Start is called before the first frame update
    void Start()
    {
        m_AudioSource = GetComponentInChildren<AudioSource>();
        m_RigidBody = GetComponent<Rigidbody>();
    }

    void PlayPenetrateIceSound()
    {
        int soundIndex = Random.Range(0, penetrateIceSounds.Length);
        m_AudioSource.PlayOneShot(penetrateIceSounds[soundIndex]);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("COLLIDE");
        if (!collision.collider.gameObject.CompareTag("Ice"))
        {
            return;
        }
        Debug.Log("PLAY");

        PlayPenetrateIceSound();

        PenetratedIce();
    }

    public void PenetratedIce()
    {
        //m_RigidBody.isKinematic = true;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [SerializeField] private PlayerLocomotion locomotion;
    [SerializeField] private AudioClip[] slippingSound;
    [SerializeField] private AudioClip[] footstepSound;
    [SerializeField] private WallAnchor floorWallAnchor;
    
    [Tooltip("This is where sounds like falling sounds will play")]
    [SerializeField] private AudioSource feetAudio;

    private Vector3 lastPosition;
    private float cumulativeMovement;
    private Stack<WallAnchor> securedWallAnchors;
    
    void Awake()
    {
        instance = this;
        lastPosition = transform.position;
        securedWallAnchors = new Stack<WallAnchor>();
        WallAnchorSecured(floorWallAnchor);
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

    public void WallAnchorSecured(WallAnchor anchor)
    {
        securedWallAnchors.Push(anchor);

        locomotion.ropeAnchor = anchor.transform;
    }

    public void WallAnchorRemoved(WallAnchor anchor)
    {
        if (!IsWallAnchorLastInChain(anchor)) return;
        securedWallAnchors.Pop();
        if(securedWallAnchors.Count > 0) { 
            locomotion.ropeAnchor = securedWallAnchors.Peek().transform;
        }
        else
        {
            locomotion.ropeAnchor = null;
        }
    }
    public bool IsWallAnchorLastInChain(WallAnchor anchor)
    {
        return anchor == securedWallAnchors.Peek();
    }

    public void PlaySlippingSound() => PlayRandomSound(slippingSound);
    void PlayRandomSound(AudioClip[] sounds)
    {
        int index = Random.Range(0, sounds.Length);
        feetAudio.PlayOneShot(sounds[index]);
    }
}

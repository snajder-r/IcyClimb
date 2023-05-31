using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class IcePick : LodgeAbleGrabbable
{
    [Header("Ice Pick")]
    
    [SerializeField] private AudioClip[] handSlipSound;
    
    private IcePickTip tip;

    /// <summary>
    /// The pull the axe currently exacts on the player. 
    /// </summary>
    public Vector3 PullPlayer
    {
        get {
            if (!(isLodged && isSelected))
            {
                // Only pull the player if the ice pick is lodged in ice and held by the player
                return Vector3.zero;
            }
            return attachTransform.position - heldController.transform.position;
        }
    }


    void Start()
    {
        tip = GetComponentInChildren<IcePickTip>();
        remainsLodgedIfReleased = false;
    }

    public void LoseGrip()
    {
        ForceRelease();
        if (heldController)
        {
            AudioSource audio = heldController.gameObject.GetComponent<AudioSource>();
            if (audio)
            {
                int index = Random.Range(0, handSlipSound.Length);
                audio.PlayOneShot(handSlipSound[index], 0.5f);
            }
        }
    }


    public override void Lodge()
    {
        base.Lodge();

        // Fire listeners
        PlayerController.instance.OnPickLodged(this);
    }

    public override void Dislodge()
    {
        base.Dislodge();

        // Fire listeners
        tip.OnDislodge();
        PlayerController.instance.OnPickDislodged(this);
    }
}

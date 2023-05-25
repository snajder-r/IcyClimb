using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private IcePick leftPick;
    [SerializeField] private IcePick rightPick;
    [SerializeField] private float staminaLeft;
    [SerializeField] private float staminaRight;
    [SerializeField] private float pullSpeed;
    [Tooltip("Pulling will slow down to zero as the distance from the hand to the PlayerController object reaches this distance (note that this may be the hand-to-feet distance)")]
    [SerializeField] private float maxHandDistance = 2.5f;
    [Tooltip("Angle relative to Vector3.UP at which a character starts to slide down a slope. 90° is flat ground.")]
    [SerializeField] private float floorAngleSlipping = 95f;
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private AudioClip[] slippingSound;
    [SerializeField] private AudioClip[] footstepSound;

    [Tooltip("This is where sounds like falling sounds will play")]
    [SerializeField] private AudioSource feetAudio;

    [SerializeField] private ActionBasedContinuousMoveProvider moveProvider;

    private CharacterController m_CharacterController;

    public static PlayerController instance {get; private set;}

    private int m_NumPicksLodged = 0;
    private int floorLayerMask;

    // Raycastings used to determine whether we should fall
    private Vector3[] fallRays;
    private float fallVelocity;
    private float recentMovement = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        // Singleton pattern
        if(instance == null)
        {
            instance = this;
        }

        m_CharacterController = GetComponent<CharacterController>();
        lastPosition = transform.position;

        SetUpFallRays();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_NumPicksLodged > 0)
        {
            UpdatePickMovement();
        }

        if(m_NumPicksLodged == 0) { 
            Fall();
        }

        PlayFootsteps();
    }

    void PlayFootsteps()
    {
        if (m_CharacterController.isGrounded && (m_NumPicksLodged == 0))
        {
            recentMovement += (transform.position - lastPosition).magnitude;
            if (recentMovement > 0.5f)
            {
                int index = Random.Range(0, footstepSound.Length);
                feetAudio.PlayOneShot(footstepSound[index]);
                recentMovement = 0f;
            }
        }
        lastPosition = transform.position;
    }

    /***********************************************
     *  FALLING
     * *********************************************/
    private void SetUpFallRays()
    {
        float stepDegrees = 30f;
        List<Vector3> rays = new List<Vector3>();
        Quaternion rotation = Quaternion.AngleAxis(30f, Vector3.up);
        //Start with one ray, and then rotate it around the y axis to create a higher resolution scan
        rays.Add(new Vector3(0.1f, -0.1f, 0f));
        for (int i = 1; i < 360f / stepDegrees; i++)
        {
            rays.Add(rotation * rays[rays.Count - 1]);
        }
        fallRays = rays.ToArray();

        floorLayerMask = LayerMask.GetMask(new string[] { "Wall" });
    }


    /// <summary>
    /// Returns the average floor normal below the player based on a 
    /// number of rays around the player. Can be used to determine whether the
    /// player stands on solid ground or a slope.
    /// </summary>
    Vector3 ScanFallRays()
    {
        Vector3 normalSum = Vector3.zero;
        foreach (Vector3 offset in fallRays)
        {
            RaycastHit hit;
            Vector3 normal = -Vector3.up;
            if (Physics.Raycast(transform.position + Vector3.up*0.1f , offset, out hit, 0.2f, floorLayerMask))
            {
                normal = hit.normal;
            }
            normalSum += normal;
        }
        Vector3 meanNormal = normalSum / fallRays.Length;
        return meanNormal;
    }

    /// <summary>
    /// Determine whether the player stands on uneven ground (or no ground at all) and make them fall if they are.
    /// </summary>
    void Fall()
    {
        Vector3 floorNormal = ScanFallRays();
        float floorAngle = Vector3.Dot(floorNormal, Vector3.up);
        if (Mathf.Acos(floorAngle) * Mathf.Rad2Deg > floorAngleSlipping)
        {
            //We're falling!

            //If this is the start of a fall and we are grounded (i.e. we are on slippery ground) play a sound
            if(fallVelocity == 0f) { 
                int sound_index = Random.Range(0, slippingSound.Length);
                feetAudio.PlayOneShot(slippingSound[sound_index]);
            }

            //velocity increases as you fall. This is a cheap approximation of increasing velocity that doesn't
            //take into account drag or torque
            fallVelocity = fallVelocity + fallSpeed * Time.deltaTime;
        }
        else if(fallVelocity != 0f)
        {
            // Don't immediately stop falling, allow to slide a little even if a single frame is stable
            fallVelocity = fallVelocity * 0.5f;
            if(fallVelocity < 0.1f)
            {
                fallVelocity = 0f;
            }
        }

        if(fallVelocity != 0f) { 
            m_CharacterController.Move(floorNormal.normalized * fallVelocity * Time.deltaTime);
        }
    }

    /***************************************************
    * PICK LOCOMOTION
    ****************************************************/

    public void OnPickLodged(IcePick pick)
    {
        m_NumPicksLodged++;

        if (m_NumPicksLodged == 1)
        {
            moveProvider.enabled = false;
        }
    }

    public void OnPickDislodged(IcePick pick)
    {
        m_NumPicksLodged--;

        if (m_NumPicksLodged == 0)
        {
            moveProvider.enabled = true;
        }
    }


    /// <summary>
    /// Returns 0 if arm is not used for pulling or arm is so 
    /// far away that it's too hard to pull up and 1 if the hand is essentially
    /// overlapping with the character controller (closer to the body)
    /// </summary>
    float StretchingPullSpeed(IcePick pick)
    {
        if(!pick.lodged || !pick.isSelected)
        {
            return 0f;
        }
        float distance = (pick.transform.position - transform.position).magnitude;
        float strainAdjustedSpeed = Mathf.Clamp(-Mathf.Log(distance/maxHandDistance), 0, 1);
        return strainAdjustedSpeed; 
    }
    void UpdatePickMovement()
    {
        Vector3 leftPull = leftPick.PullPlayer;
        Vector3 rightPull = rightPick.PullPlayer;

        // Adjust pull for how far the arm is from the body
        leftPull *= StretchingPullSpeed(leftPick);
        rightPull *= StretchingPullSpeed(rightPick);

        // This will pull in the mean direction, but using the pull power from both hands
        Vector3 pull = leftPull + rightPull;

        m_CharacterController.Move(pull * Time.deltaTime * pullSpeed);
    }
}

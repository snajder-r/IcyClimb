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
    [SerializeField] private ActionBasedContinuousMoveProvider moveProvider;

    private CharacterController m_CharacterController;

    public static PlayerController instance {get; private set;}

    private int m_NumPicksLodged = 0;

    void Start()
    {
        // Singleton pattern
        if(instance == null)
        {
            instance = this;
        }

        m_CharacterController = GetComponent<CharacterController>();
    }

    public void OnPickLodged(IcePick pick)
    {
        m_NumPicksLodged++;

        if(m_NumPicksLodged == 1)
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

    // Update is called once per frame
    void Update()
    {
        if (m_NumPicksLodged > 0)
        {
            UpdatePickMovement();
        }
    }

    void UpdatePickMovement()
    {
        Vector3 leftPull = leftPick.PullPlayer;
        Vector3 rightPull = rightPick.PullPlayer;
        Vector3 pull = leftPull + rightPull;
        if(leftPull.magnitude * rightPull.magnitude != 0f)
        {
            pull /= 2f;
        }
        
        m_CharacterController.Move(pull * Time.deltaTime * pullSpeed);
    }
}

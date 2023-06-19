using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class Hand : XRDirectInteractor, IPullProvider
{
    [SerializeField] Transform playerCenterOfGravity;
    [SerializeField] Animator animator;
    [SerializeField] float maxStamina;
    [SerializeField] float staminaRegeneration;
    [SerializeField] float staminaBoostIncrement;
    [SerializeField] float staminaBoostCooldownTime;
    [SerializeField] float pullStrength = 1f;
    
    [Tooltip("Pulling will slow down to zero as the distance from the hand to the PlayerController object reaches this distance (note that this may be the hand-to-feet distance)")]
    [SerializeField] float maxHandDistance;

    [SerializeField] UnityEvent<float> staminaChanged;

    [SerializeField] ParticleSystem staminaBoostEffect;

    Cooldown staminaBoostCooldown;

    float _stamina;
    [SerializeField]
    float stamina { get => _stamina; 
        set {
            _stamina = value;
            staminaChanged.Invoke(_stamina / maxStamina);
        } 
    }

    [field: SerializeField]
    private DropablePully heldPully;
    public Vector3 GetPull()
    {
        Vector3 ret = Vector3.zero;
        if (heldPully is null) return ret;
        // Reduce stamina any time Pull is queried
        Vector3 pullyPull = heldPully.GetPull() * pullStrength;
        stamina -= pullyPull.magnitude * Time.deltaTime;
        pullyPull *= GetStretchingPullSpeed();
        ret += pullyPull;
        return ret;
    }



    float GetStretchingPullSpeed()
    {
        float distance = (heldPully.transform.position - playerCenterOfGravity.position).magnitude;
        float strainAdjustedSpeed = Mathf.Clamp(-Mathf.Log(distance / maxHandDistance), 0, 1);
        return strainAdjustedSpeed;
    }

    public bool IsSecured()
    {
        return (heldPully is not null) && heldPully.IsSecured();
    }


    protected override void Awake()
    {
        base.Awake();
        stamina = maxStamina;
        staminaBoostCooldown = new Cooldown(staminaBoostCooldownTime);
    }
   
    void Update()
    {
        if (heldPully is null && stamina < maxStamina)
        {
            
            stamina += staminaRegeneration * Time.deltaTime;
        }

        float stamina_ratio = stamina / maxStamina;
        if(stamina_ratio < 0.25)
        {
            xrController.SendHapticImpulse(1f - stamina_ratio * 4f, 0.5f);
        }

        if(stamina < 0f)
        {
            OnOutOfStamina();
        }

        animator.SetBool("b_grab", xrController.selectInteractionState.active);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (args.interactableObject is DropablePully)
        {
            heldPully = (DropablePully) args.interactableObject;
        }

        
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        heldPully = null;
    }

    

    public void OnOutOfStamina()
    {
        if (heldPully) heldPully.OnOutOfStamina();
    }

    public void StaminaBoost()
    {
        // No need to regenerate if we are nearly full
        if (stamina > maxStamina - staminaBoostIncrement * 0.5f) return;

        // Can't regenerate if we are holding something
        if (heldPully) return;

        if (staminaBoostCooldown.Acquire()) { 
            stamina += staminaBoostIncrement;
            staminaBoostEffect.Play();
        }
    }

}

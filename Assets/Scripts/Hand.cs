using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// A player's hand acts as an interactor for grabbing things, the source of pulling locomotion, as well as hand-specific stamina management
/// </summary>
public class Hand : XRDirectInteractor, IPullProvider
{
    #region "Fields"
    [Tooltip("Reference from which hand-to-body distance is calculated")]
    [SerializeField]
    Transform _playerCenterOfGravity;
    [SerializeField]
    Animator _animator;
    [Tooltip("Maximum stamina value")]
    [SerializeField]
    float _maxStamina;
    [Tooltip("Stamina regeneration per second")]
    [SerializeField]
    float _staminaRegeneration;
    [Tooltip("Stamina boost when shaking hand")]
    [SerializeField]
    float _staminaBoostIncrement;
    [Tooltip("Speed multiplier for pulling yourself towards an object. I recommend not to make this too large, as you might overshoot which could make you bounce back and forth. If you want to increase the speed, look at the maxHandDistance property instead.")]
    [SerializeField]
    float _pullStrength = 1f;

    [Tooltip("Pulling will slow down to zero as the distance from the hand to the center of gravity reaches this distance (note that this may be the hand-to-feet distance)")]
    [SerializeField]
    float _maxHandDistance;

    [SerializeField]
    UnityEvent<float> _onStaminaChanged;

    [Tooltip("Visual effect to trigger when stamina is boosted by shaking your hand")]
    [SerializeField]
    ParticleSystem _staminaBoostEffect;

    [Tooltip("Number of seconds between possible stamina boosts by shaking your hand")]
    [SerializeField]
    float _staminaBoostCooldownTime;

    /// <summary>
    /// Cooldown for stamina regeneration boost by shaking your hand
    /// </summary>
    Cooldown _staminaBoostCooldown;

    /// <summary>
    /// This stores the pully that is currently held by this hand
    /// </summary>
    DropablePully _heldPully;
    #endregion

    #region "Properties"
    public bool IsSecured
    {
        get => (_heldPully is not null) && _heldPully.IsSecured;
    }

    /// <summary>
    /// Current stamina in this hand
    /// </summary>
    float _stamina;
    float stamina
    {
        get => _stamina;
        set
        {
            _stamina = value;
            _onStaminaChanged.Invoke(_stamina / _maxStamina);
        }
    }

    /// <summary>
    /// The multiplier to pulling speed computed based on how far the hand is from the body
    /// If the hand is further from the body, pulling slows down, until it eventually reaches zero
    /// </summary>
    float _stretchingPullSpeedModifier
    {
        get
        {
            float distance = (_heldPully.transform.position - _playerCenterOfGravity.position).magnitude;
            float strainAdjustedSpeed = Mathf.Clamp(-Mathf.Log(distance / _maxHandDistance), 0, 1);
            return strainAdjustedSpeed;
        }
    }
    #endregion

    #region "Lifecycle Methods"
    protected override void Awake()
    {
        base.Awake();
        stamina = _maxStamina;
        _staminaBoostCooldown = new Cooldown(_staminaBoostCooldownTime);
    }

    void Update()
    {
        if (_heldPully is null && stamina < _maxStamina)
        {
            // Regenerate stamina if we aren't holding anything in our hand
            stamina += _staminaRegeneration * Time.deltaTime;
        }

        float stamina_ratio = stamina / _maxStamina;
        if (stamina_ratio < 0.25)
        {
            // Vibrate if we are below 25% stamina and increase the vibration strength as it approaches 0% stamina
            xrController.SendHapticImpulse(1f - stamina_ratio * 4f, 0.5f);
        }

        if (stamina < 0f)
        {
            // If stamina reaches 0
            OnOutOfStamina();
        }

        // Let the animation controller know whether we are currently pressing the grab button
        // The an
        _animator.SetBool("b_grab", xrController.selectInteractionState.active);
    }
    #endregion

    #region "Events"
    public void OnOutOfStamina()
    {
        // Forward the out of stamina handling (will result in a dropable pully being dropped)
        if (_heldPully) _heldPully.OnOutOfStamina();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (args.interactableObject is DropablePully)
        {
            // Store the pully we just grabbed
            _heldPully = (DropablePully)args.interactableObject;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        // Delete the reference to a potential pully we might have grabbed
        _heldPully = null;
    }
    #endregion


    #region "Methods"
    /// <summary>
    /// Computes pull based on the held pully and reduces stamina when actually pulling something
    /// </summary>
    /// <returns>The direction in which this hand pulls</returns>
    public Vector3 Pull()
    {
        Vector3 ret = Vector3.zero;
        if (_heldPully is null) return ret;
        // Reduce stamina any time Pull is queried
        Vector3 pullyPull = _heldPully.Pull() * _pullStrength;
        stamina -= pullyPull.magnitude * Time.deltaTime;
        pullyPull *= _stretchingPullSpeedModifier;
        ret += pullyPull;
        return ret;
    }
    /// <summary>
    /// Perform an stamina boost if the cooldown allows it
    /// </summary>
    public void StaminaBoost()
    {
        // No need to regenerate if we are nearly full
        if (stamina > _maxStamina - _staminaBoostIncrement * 0.5f) return;

        // Can't regenerate if we are holding something
        if (_heldPully) return;

        // Check if it is on cooldown
        if (_staminaBoostCooldown.Acquire())
        {
            // If not on cooldown, stamina is being boosted
            stamina += _staminaBoostIncrement;
            // Then play the stamina boost animation
            _staminaBoostEffect.Play();
        }
    }
    #endregion
}

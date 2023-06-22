using UnityEngine;

/// <summary>
/// Useful class for managing actions with cooldowns
/// </summary>
public class Cooldown
{
    // The time between activations.
    private readonly float _cooldownTime;
    // The time when the last cooldown has started
    private float _started;

    /// <param name="cooldownTime"> The time between activations in seconds </param>
    public Cooldown(float cooldownTime)
    {
        _started = Time.time;
        _cooldownTime = cooldownTime;
    }

    /// <summary>
    /// Test whether we are currently waiting on a cooldown.
    /// If we are currently waiting for a cooldown, returns false.
    /// If we are not currently waiting for a cooldown, returns true and starts waiting for a cooldown.
    /// </summary>
    /// <returns>Whether we are currently allowed to perform the associated action</returns>
    public bool Acquire()
    {
        if(Time.time - _started >= _cooldownTime)
        {
            _started = Time.time;
            return true;
        }
        return false;
    }
}

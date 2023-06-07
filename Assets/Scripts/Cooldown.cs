using UnityEngine;

public class Cooldown
{
    private float cooldownTime;
    private float started;

    public Cooldown(float cooldown)
    {
        started = Time.time;
        cooldownTime = cooldown;
    }

    public bool Acquire()
    {
        if(Time.time - started >= cooldownTime)
        {
            started = Time.time;
            return true;
        }
        return false;
    }
}

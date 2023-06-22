using UnityEngine;
/// <summary>
/// Interface for anything that can pull or secure the player
/// </summary>
public interface IPullProvider
{
    /// <summary>Whether this provider currently secures the player from falling</summary>
    public abstract bool IsSecured { get; }

    /// <returns>The pulling force and direction excerted by this provider</returns>
    public abstract Vector3 Pull();

    /// <summary>Deals with running out of stamina. Typically release the pully </summary>
    public abstract void OnOutOfStamina();
}

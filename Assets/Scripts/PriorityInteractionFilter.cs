using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

/// <summary>
/// This is an IXRSelectFilter which can be used to determine which XRGrabInteractables 
/// should be prioritized over others when the player is hovering multiple at the same time.
/// Simply add this to StartingSelectFilters of your XRInteractionManager and then add 
/// all selectables with priorities higher than normal to the priority list. 
/// </summary>
public class PriorityInteractionFilter : MonoBehaviour, IXRSelectFilter
{

    [SerializeField, Tooltip("All XRGrabInteractables which should be prioritized first, in order of priority. Any SelectInteractables which aren't listed here will still be selectable, but lower priority than those listed here.")]
    List<XRGrabInteractable> _highPriorityList;

    [SerializeField, Tooltip("All XRGrabInteractables which should be prioritized last, in order of priority. Any SelectInteractables which aren't listed here will still be selectable, but have a higher priority than those listed here.")]
    List<XRGrabInteractable> _lowPriorityList;

    public bool canProcess => true;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactor is not IXRHoverInteractor) return true;

        int toSelectPriority = GetPriorityOfObject(interactable);

        List<IXRHoverInteractable> hoveredItems = ((IXRHoverInteractor)interactor).interactablesHovered;
        foreach (IXRHoverInteractable hoveredItem in hoveredItems)
        {
            if (hoveredItem is not XRGrabInteractable) continue;
            if (!_highPriorityList.Contains((XRGrabInteractable)hoveredItem)) continue;

            int hoveredPriority = GetPriorityOfObject(hoveredItem);
            if (hoveredPriority > toSelectPriority)
            {
                // We are currently hovering something with higher priority, so no
                return false;
            }
        }
        // We're not hovering anything with higher priority, so you can go ahead
        return true;
    }

    private int GetPriorityOfObject(IXRInteractable interactable)
    {
        //default priority is zero
        int priority = 0;
        if (interactable is XRGrabInteractable grabInteractable)
        {
            if (_highPriorityList.Contains(grabInteractable))
            {
                // Positive priority
                priority = _highPriorityList.Count - _highPriorityList.IndexOf(grabInteractable);
            }
            else if (_lowPriorityList.Contains(grabInteractable))
            {
                // Negative priority
                priority = - _lowPriorityList.IndexOf(grabInteractable);
            }
        }
        return priority;
    }
}

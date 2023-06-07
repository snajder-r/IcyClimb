using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

public class PriorityInteractionFilter : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] List<XRGrabInteractable> priorityList;

    public bool canProcess => true;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactor is not IXRHoverInteractor) return true;

        //default priority is lowest
        int myPriority = priorityList.Count;
        if(interactable is XRGrabInteractable)
        {
            if (priorityList.Contains((XRGrabInteractable)interactable))
            {
                myPriority = priorityList.IndexOf((XRGrabInteractable)interactable);
            }
        }
        

        List<IXRHoverInteractable> hoveredItems = ((IXRHoverInteractor)interactor).interactablesHovered;
        foreach(IXRHoverInteractable hoveredItem in hoveredItems)
        {
            if (hoveredItem is not XRGrabInteractable) continue;
            if (!priorityList.Contains((XRGrabInteractable)hoveredItem)) continue;
            
            int hoveredPriority = priorityList.IndexOf((XRGrabInteractable)hoveredItem);
            if (hoveredPriority < myPriority)
            {
                // We are currently hovering something with higher priority, so no
                return false;
            }
        }
        // We're not hovering anything with higher priority, so you can go ahead
        return true;
    }
}

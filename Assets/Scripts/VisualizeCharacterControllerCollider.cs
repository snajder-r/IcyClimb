using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizeCharacterControllerCollider : MonoBehaviour
{
    [SerializeField] CharacterController controller;
    void Start()
    {
        
    }

    
    void Update()
    {
        transform.position = controller.transform.position + controller.transform.rotation*controller.center;
        transform.rotation = controller.transform.rotation * controller.transform.rotation;
        transform.localScale = new Vector3(controller.radius*2f, controller.height/2f, controller.radius*2f);
    }
}

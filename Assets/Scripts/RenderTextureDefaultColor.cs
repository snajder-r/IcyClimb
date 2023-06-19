using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTextureDefaultColor : MonoBehaviour
{
    [SerializeField] Color defaultColor;


    void Start()
    {
        RenderTexture.active = GetComponent<Camera>().targetTexture;
        GL.Clear(true, true, defaultColor);
        RenderTexture.active = null;
    }

    
    void Update()
    {

    }
}

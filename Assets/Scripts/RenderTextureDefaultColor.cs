using UnityEngine;


/// <summary>
/// Used to set the default color of a render texture. 
/// Apply this component to the camera which writes to the render texture
/// </summary>
public class RenderTextureDefaultColor : MonoBehaviour
{
    [SerializeField] 
    Color _defaultColor;
    void Start()
    {
        RenderTexture.active = GetComponent<Camera>().targetTexture;
        GL.Clear(true, true, _defaultColor);
        RenderTexture.active = null;
    }
}

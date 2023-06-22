using UnityEngine;

/// <summary>
/// Triggers a camera to turn on for 1 frame, in order to take a picture for a render texture
/// </summary>
public class PhotoCam : MonoBehaviour
{
    [SerializeField]
    Camera _photoCamera;

    /// <summary>
    /// At count 2 the picture will be taken in the next update. 
    /// At count 1 the camera will be disabled forever and this behavior also stops updating
    /// </summary>
    int _disableInFrames = 0;
    void Start()
    {
        _photoCamera.enabled = false;
    }
    private void Update()
    {
        if (_disableInFrames == 2)
        {
            _disableInFrames--;
        }
        else if (_disableInFrames == 1)
        {
            DisableForever();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // Take a picture when the trigger entered
        Invoke(nameof(TakePicture), 0.5f);
    }

    /// <summary>
    /// Enable the camera and set the "countdown" for when it should be disabled
    /// </summary>
    private void TakePicture()
    {
        _photoCamera.enabled = true;
        // Make sure we have least 1 frame where the pic is taken.
        _disableInFrames = 2;
    }

    /// <summary>
    /// Disables both the camera object as well as this entire gameobject so that this will never be executed again
    /// </summary>
    private void DisableForever()
    {
        _photoCamera.enabled = false;
        gameObject.SetActive(false);
    }
}

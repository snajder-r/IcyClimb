using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoCam : MonoBehaviour
{
    [SerializeField] Camera photoCamera;

    private int disableInFrames = 0;

    void Start()
    {
        photoCamera.enabled = false;
    }

    private void Update()
    {
        if(disableInFrames == 2)
        {
            disableInFrames--;
        }else if(disableInFrames == 1)
        {
            DisableForever();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Invoke("TakePicture", 0.5f);
    }

    void TakePicture()
    {
        photoCamera.enabled = true;
        // Make sure we have least 1 frame where the pic is taken
        disableInFrames = 2;
    }

    void DisableForever()
    {
        photoCamera.enabled = false;
        gameObject.SetActive(false);
    }
}

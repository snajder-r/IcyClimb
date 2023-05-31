using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public static ContactPoint[] ContactPointBuffer = new ContactPoint[20];


    void Start()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

}

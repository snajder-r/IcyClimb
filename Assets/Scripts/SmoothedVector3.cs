using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothedVector3
{
    private Vector3[] samples;
    private int index;

    public SmoothedVector3(int windowSize)
    {
        samples = new Vector3[windowSize];
        index = 0;
    }

    public void Add(Vector3 sample)
    {
        samples[index++] = sample;
        index = index % samples.Length;
    }

    public Vector3 Mean { 
        get {
                Vector3 sum = Vector3.zero;
                foreach(Vector3 a in samples)
                {
                    sum += a;
                }

                return sum / samples.Length;
            } 
    }
}

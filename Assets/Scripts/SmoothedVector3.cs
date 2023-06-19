using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MODE
{
    ArithmeticMean, MedianMagnitude
}
public class SmoothedVector3
{
    private Vector3[] samples;
    private int index;
    private MODE meanMode;

    public SmoothedVector3(int windowSize) : this(windowSize, Vector3.zero) { }

    public SmoothedVector3(int windowSize, Vector3 defaultContent) : this(windowSize, defaultContent, MODE.ArithmeticMean) { }

    public SmoothedVector3(int windowSize, Vector3 defaultContent, MODE mode)
    {
        samples = new Vector3[windowSize];
        index = 0;

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = defaultContent;
        }
        meanMode = mode;
    }



    public void Add(Vector3 sample)
    {
        samples[index++] = sample;
        index = index % samples.Length;
    }

    public Vector3 Mean
    {
        get
        {
            switch (meanMode)
            {
                case MODE.MedianMagnitude: return CalculateMedianMagnitude(samples);
                default:
                case MODE.ArithmeticMean: return CalculateMean(samples);
            }
        }
    }

    private Vector3 CalculateMean(IEnumerable<Vector3> points)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 a in points)
        {
            sum += a;
        }

        return sum / samples.Length;
    }

    private Vector3 CalculateMedianMagnitude(IEnumerable<Vector3> points)
    {
        List<Vector3> pointList = new List<Vector3>(points);
        pointList.Sort((a, b) => a.magnitude.CompareTo(b.magnitude));

        int medianIndex = pointList.Count / 2;
        return pointList[medianIndex];
    }

    public Vector3[] BinnedMeans(int bins)
    {
        // Int Ceil division. Note that (a+b-1)/b is equivalent to (int)Mathf.ceil(((float)a)/((float)b))
        int perBin = (samples.Length + bins - 1) / bins;
        Vector3[] binnedMeans = new Vector3[bins];

        for (int i = 0; i < bins; i++)
        {
            binnedMeans[i] = CalculateMean(GetSamples(i, i + perBin));
        }
        return binnedMeans;
    }

    private IEnumerable<Vector3> GetSamples(int start, int end)
    {
        for (int i = start; i < end && i < samples.Length; i++)
        {
            yield return samples[i];
        }
    }

}

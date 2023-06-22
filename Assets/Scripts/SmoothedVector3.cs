using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class allows collecting samples of Vector3 in a ringbuffer, and methods for computing the average of all samples
/// </summary>
public class SmoothedVector3
{
    public enum Mode
    {
        ArithmeticMean, MedianMagnitude
    }

    /// <summary>
    /// Ringbuffer storing samples
    /// </summary>
    private Vector3[] _samples;
    /// <summary>
    /// Current position in ringbuffer
    /// </summary>
    private int _index;
    /// <summary>
    /// Which method should be used for computing the mean
    /// </summary>
    private Mode _meanMode;

    public SmoothedVector3(int windowSize) : this(windowSize, Vector3.zero) { }

    public SmoothedVector3(int windowSize, Vector3 defaultContent) : this(windowSize, defaultContent, Mode.ArithmeticMean) { }

    /// <param name="windowSize">The size of the ringbuffer, thus the number of samples the average should be computed on</param>
    /// <param name="defaultContent">Initial values for the ringbuffer</param>
    /// <param name="mode">The averaging method</param>
    public SmoothedVector3(int windowSize, Vector3 defaultContent, Mode mode)
    {
        _samples = new Vector3[windowSize];
        _index = 0;

        for (int i = 0; i < _samples.Length; i++)
        {
            _samples[i] = defaultContent;
        }
        _meanMode = mode;
    }

    /// <summary>
    /// Add a sample to the ringbuffer
    /// </summary>
    public void Add(Vector3 sample)
    {
        _samples[_index++] = sample;
        _index = _index % _samples.Length;
    }

    /// <summary>
    /// Return the mean of all samples in the buffer
    /// </summary>
    public Vector3 Mean
    {
        get
        {
            return _meanMode switch
            {
                Mode.MedianMagnitude => MathUtils.MedianMagnitudeVector(_samples),
                Mode.ArithmeticMean => MathUtils.MeanVector(_samples),
                _ => MathUtils.MeanVector(_samples)
            };
        }
    }

    /// <summary>
    /// Splits ringbuffer into a number of bins and computes arithmetic mean for each bin
    /// </summary>
    public Vector3[] BinnedMeans(int bins)
    {
        // Int Ceil division. Note that (a+b-1)/b is equivalent to (int)Mathf.ceil(((float)a)/((float)b))
        int perBin = (_samples.Length + bins - 1) / bins;
        Vector3[] binnedMeans = new Vector3[bins];

        for (int i = 0; i < bins; i++)
        {
            binnedMeans[i] = MathUtils.MeanVector(GetSamples(i, i + perBin));
        }
        return binnedMeans;
    }

    /// <summary>
    /// Maps indices such that if parameter is 0 you get the oldest sample and 
    /// if parameter is the ringbuffer length minus one you get the newest sample
    /// </summary>
    private int MapIndexToRingbuffer(int index)
    {
        return (index + _index) % _samples.Length;
    }

    /// <summary>
    /// Enumerator of samples from ringbuffer
    /// </summary>
    /// <param name="start">
    /// start as <b>in chronological index</b> meaning 
    /// if start=0 it starts from the oldest sample
    /// </param>
    /// <param name="end">
    /// end as <b>in chronological index</b> meaning 
    /// if end=length of ringbuffer it ends with the newest sample
    /// </param>
    private IEnumerable<Vector3> GetSamples(int start, int end)
    {
        for (int i = start; i < end && i < _samples.Length; i++)
        {
            int index = MapIndexToRingbuffer(i);
            yield return _samples[index];
        }
    }
}

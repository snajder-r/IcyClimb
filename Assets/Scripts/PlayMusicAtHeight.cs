using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayMusicAtHeight : MonoBehaviour
{
    [SerializeField] private TMP_Text[] creditText;
    [SerializeField] private float creditFadeInTime = 5f;
    [SerializeField] private float holdCreditsSeconds = 5f;

    [SerializeField] private AudioClip audioClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float musicFadeInTime = 5f;

    [SerializeField] private float playAtHeight = 80f;

    private float m_CreditsFading = 0f;
    private float m_MusicFading = 0f;
    private bool m_HavePlayed = false;

    void Start()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        foreach(TMP_Text tmp in creditText)
        {
            Color textColor = tmp.color;
            textColor.a = 0f;
            tmp.color = textColor;
        }  
    }


    public void Update()
    {
        if (m_CreditsFading != 0f)
        {
            FadeCredit();
        }
        if (m_MusicFading != 0f)
        {
            FadeMusic();
        }

        if(transform.position.y > playAtHeight && !m_HavePlayed)
        {
            Play();
        }

        if(m_HavePlayed && !audioSource.isPlaying)
        {
            // We are done playing. Since this component only plays once, let's just disable ourselves
            Destroy(this);
        }
    }

    public void Play()
    {
        m_CreditsFading = 1f / creditFadeInTime;

        audioSource.PlayOneShot(audioClip, 1f);
        audioSource.volume = 0f;

        m_MusicFading = 1f / musicFadeInTime;

        m_HavePlayed = true;
    }


    public void FadeCredit()
    {
        foreach (TMP_Text tmp in creditText)
        {
            Color textColor = tmp.color;
            textColor.a += m_CreditsFading * Time.deltaTime;
            tmp.color = textColor;
            if (tmp.color.a >= 1f && m_CreditsFading > 0f)
            {
                m_CreditsFading = 0f;
                //Schedule fading out credits
                Invoke("StartCreditFadeout", holdCreditsSeconds);
            }
            if (tmp.color.a <= 0f && m_CreditsFading < 0f)
            {
                m_CreditsFading = 0f;
            }
        }

    }

    private void StartCreditFadeout()
    {
        m_CreditsFading = -1f / creditFadeInTime;
    }
    public void FadeMusic()
    {
        audioSource.volume += m_MusicFading * Time.deltaTime;

        if (audioSource.volume >= 1f && m_MusicFading > 0f)
        {
            m_MusicFading = 0f;
        }
        if (audioSource.volume <= 0f && m_MusicFading < 0f)
        {
            m_MusicFading = 0f;
        }
    }
}

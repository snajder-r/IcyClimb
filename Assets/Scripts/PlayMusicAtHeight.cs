using UnityEngine.Serialization;
using UnityEngine;
using TMPro;

/// <summary>
/// Fade in music and music credits when the player reaches a certain height
/// </summary>
public class PlayMusicAtHeight : MonoBehaviour
{
    [SerializeField, Tooltip("Text objects where the credits are displayed")]
    private TMP_Text[] _creditText;
    [SerializeField, Tooltip("How long it takes to fade in and out the credits")]
    private float _creditFadeInTime = 5f;
    [SerializeField, Tooltip("How long the credits should be visibile after they faded in and before they fade out")]
    private float _holdCreditsSeconds = 5f;

    [SerializeField]
    private AudioClip _audioClip;
    [SerializeField]
    private AudioSource _audioSource;
    [SerializeField, Tooltip("How long it takes to fade in the music")]
    private float _musicFadeInTime = 5f;

    [SerializeField, Tooltip("The height at which the song should be played")]
    private float _playAtHeight = 80f;

    /// <summary>
    /// Current opacity increment for fading. 
    /// Should be postive while fading in, 0 while holding, and negative while fading out
    /// </summary>
    private float _creditsFading = 0f;
    /// <summary>
    /// Volume increment for fading. 
    /// Should be postive while fading in, then 0 once max volume is reached
    /// </summary>
    private float _musicFading = 0f;
    /// <summary>
    /// We only want to play once, so this indicates that we are done
    /// </summary>
    private bool _havePlayed = false;

    void Start()
    {
        if (!_audioSource) _audioSource = GetComponent<AudioSource>();

        foreach (TMP_Text tmp in _creditText)
        {
            Color textColor = tmp.color;
            textColor.a = 0f;
            tmp.color = textColor;
        }
    }

    public void Update()
    {
        if (_creditsFading != 0f)
        {
            FadeCredit();
        }
        if (_musicFading != 0f)
        {
            FadeMusic();
        }

        if (transform.position.y > _playAtHeight && !_havePlayed)
        {
            Play();
        }

        if (_havePlayed && !_audioSource.isPlaying)
        {
            // We are done playing. Since this component only plays once, let's just disable ourselves
            Destroy(this);
        }
    }

    /// <summary>
    /// Start fading in the music and credits
    /// </summary>
    void Play()
    {
        //Initialize the _creditsFading field for fade-in
        _creditsFading = 1f / _creditFadeInTime;

        _audioSource.PlayOneShot(_audioClip, 1f);
        _audioSource.volume = 0f;

        _musicFading = 1f / _musicFadeInTime;

        _havePlayed = true;
    }

    /// <summary>
    /// Adjust credit opacity for fading in or fading out, dependin gon the _creditsFading field.
    /// </summary>
    void FadeCredit()
    {
        foreach (TMP_Text tmp in _creditText)
        {
            Color textColor = tmp.color;
            textColor.a += _creditsFading * Time.deltaTime;
            tmp.color = textColor;
            if (tmp.color.a >= 1f && _creditsFading > 0f)
            {
                _creditsFading = 0f;
                //Schedule fading out credits
                Invoke(nameof(StartCreditFadeout), _holdCreditsSeconds);
            }
            if (tmp.color.a <= 0f && _creditsFading < 0f)
            {
                _creditsFading = 0f;
            }
        }

    }

    /// <summary>
    /// Initialize the _creditsFading field for fade-out
    /// </summary>
    private void StartCreditFadeout()
    {
        _creditsFading = -1f / _creditFadeInTime;
    }

    /// <summary>
    /// Fade in the music based on _musicFading field or set _musicFading field to zero once max volume is reached
    /// </summary>
    public void FadeMusic()
    {
        _audioSource.volume += _musicFading * Time.deltaTime;

        if (_audioSource.volume >= 1f && _musicFading > 0f)
        {
            _musicFading = 0f;
        }
        if (_audioSource.volume <= 0f && _musicFading < 0f)
        {
            _musicFading = 0f;
        }
    }
}

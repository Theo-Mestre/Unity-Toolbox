using DG.Tweening;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

public partial class AudioManager : MonoBehaviour
{
    #region Singleton Implementation
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<AudioManager>();
#else
                instance = FindObjectOfType<AudioManager>();
#endif
                if (instance == null)
                {
                    var obj = new GameObject(nameof(AudioManager));
                    instance = obj.AddComponent<AudioManager>();
                }

                DontDestroyOnLoad(instance.gameObject);
                instance.Init();
            }

            return instance;
        }
    }

    public static bool IsValid() => instance != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[AudioManager.Awake] Destroying duplicate instance of AudioManager.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Shutdown();
            instance = null;
        }
    }
    #endregion

    #region Fields
    [Header("Audio Data")]
    [SerializeField, NotNull] private AudioData audioData = null;
    [SerializeField] private string noTrackPlayingString = "No Track";

    [Header("Music Fade Settings")]
    [SerializeField] private float musicFadeDuration = 1f;
    [SerializeField] private Ease trackFadeEase = Ease.InOutCubic;

    private Dictionary<string, AudioData.SoundBank> soundbanksMap = null;
    private float sfxVolume = 0.7f;
    private bool isPlaying = false;

    // Music state
    private AudioData.MusicTrack currentTrack = null;
    private Dictionary<string, AudioData.MusicBank> musicBanksMap = null;
    private string currentMusicBank = null;

    // Audio Emitters
    private AudioSource sfxSource = null;
    private AudioSource musicSource = null;

    public float MusicVolume => audioData != null ? audioData.GlobalMusicVolume : 0.7f;
    public float TrackVolume => MusicVolume * (currentTrack == null ? 1.0f : currentTrack.Volume);
    public float SFXVolume => sfxVolume;
    #endregion

    protected virtual void Init()
    {
        SetupAudioSources();
        BuildBankMap();

        isPlaying = audioData.AutoStartMusic;
    }

    protected virtual void Shutdown()
    {
        sfxSource.Stop();
        musicSource.Stop();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (musicSource.isPlaying) PauseCurrentTrack();
            else PlayNextTrack();
        }
#endif
        if (isPlaying && musicSource != null && !musicSource.isPlaying)
            PlayNextTrack();
    }

    #region Setup 
    private void SetupAudioSources()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.volume = audioData != null ? audioData.GlobalMusicVolume : 0.5f;
        musicSource.loop = false;
    }

    private void BuildBankMap()
    {
        soundbanksMap = new();
        if (audioData.SoundBanks == null) return;

        foreach (var bank in audioData.SoundBanks)
            if (!string.IsNullOrEmpty(bank.Name))
                soundbanksMap[bank.Name] = bank;

        musicBanksMap = new();
        if (audioData.MusicBanks == null) return;

        foreach (var bank in audioData.MusicBanks)
            if (!string.IsNullOrEmpty(bank.Name))
                musicBanksMap[bank.Name] = bank;
    }
    #endregion

    #region Sound Effects API
    /// <summary>Plays a random clip from the named sound bank.</summary>
    public void PlaySound(string bankName)
    {
        if (!soundbanksMap.TryGetValue(bankName, out AudioData.SoundBank bank))
        {
            Log.Warn($"No sound bank found with name '{bankName}'");
            return;
        }

        if (bank.Clips == null || bank.Clips.Length == 0)
        {
            Log.Warn($"Sound bank '{bankName}' has no clips.");
            return;
        }

        AudioClip clip = bank.Clips[Random.Range(0, bank.Clips.Length)];

        sfxSource.pitch = bank.applyRandomPitch ?
            Random.Range(0.8f, 1.2f) * bank.PitchVariation :
            bank.PitchVariation;

        sfxSource.PlayOneShot(clip, bank.Volume);
    }
    #endregion

    #region Music API

    // Pause, Resume, Toggle
    public void PauseCurrentTrack()
    {
        if (isPlaying == false) return;

        isPlaying = false;

        // reset any ongoing fade
        musicSource.DOKill();
        musicSource.DOFade(0.0f, musicFadeDuration)
            .SetEase(trackFadeEase)
            .OnComplete(() => musicSource.Stop());
    }
    public void ResumeCurrentTrack()
    {
        if (isPlaying || string.IsNullOrEmpty(currentMusicBank)) return;

        if (musicSource.clip == null || currentTrack == null)
        {
            PlayTrackFromBank(currentMusicBank);
            return;
        }
        isPlaying = true;

        // reset any ongoing fade
        musicSource.volume = 0;
        musicSource.DOKill();

        musicSource.Play();
        musicSource.DOFade(TrackVolume, musicFadeDuration)
            .SetEase(trackFadeEase);
    }
    public void ToggleCurrentTrack()
    {
        if (isPlaying) PauseCurrentTrack();
        else ResumeCurrentTrack();
    }

    // Play Methods
    public void PlayTrackFromBank(string bankName)
    {
        currentMusicBank = bankName;

        if (currentMusicBank == null ||
            !musicBanksMap.TryGetValue(currentMusicBank, out AudioData.MusicBank bank))
        {
            Log.Warn($"No sound bank found with name '{currentMusicBank}'");
            currentMusicBank = null;
            return;
        }

        PlayTrack(bank.Tracks[0]);
    }
    public void PlayRandomTrackFromBank(string bankName)
    {
        currentMusicBank = bankName;

        if (currentMusicBank == null ||
            !musicBanksMap.TryGetValue(currentMusicBank, out AudioData.MusicBank bank))
        {
            Log.Warn($"No sound bank found with name '{currentMusicBank}'");
            currentMusicBank = null;
            return;
        }

        PlayTrack(bank.Tracks[Random.Range(0, bank.Tracks.Length)]);
    }
    public void PlayNextTrack()
    {
        if (currentMusicBank == null) return;

        var list = musicBanksMap[currentMusicBank].Tracks;
        int id = Array.IndexOf(list, currentTrack) + 1;
        id %= list.Length;

        PlayTrack(list[id]);
    }
    private void PlayTrack(AudioData.MusicTrack track)
    {
        currentTrack = track;

        musicSource.clip = currentTrack.Clip;
        musicSource.volume = 0;
        musicSource.DOFade(TrackVolume, musicFadeDuration)
            .SetEase(trackFadeEase);
        musicSource.Play();
        isPlaying = true;

        EventDispatcher.Broadcast(Events.OnAudioChanged, new("AudioName", currentTrack.Title));
    }
    #endregion

    #region Helpers
    public string GetCurrentTrackName()
    {
        if (currentTrack == null)
            return noTrackPlayingString;

        return currentTrack.Title;
    }
    public void SetMusicVolume(float volume)
    {
        audioData.GlobalMusicVolume = Mathf.Clamp01(volume);
        musicSource.volume = audioData.GlobalMusicVolume;
    }
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
    #endregion
}
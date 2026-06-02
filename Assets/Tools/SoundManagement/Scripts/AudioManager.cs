using System.Collections.Generic;
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
    [SerializeField, NotNull] private AudioData audioData;
    [SerializeField] private string noTrackPlayingString = "No Track";

    private Dictionary<string, AudioData.SoundBank> bankMap;
    private readonly Stack<int> trackHistory = new();
    private int currentTrackIndex = 0;
    private float sfxVolume = 0.7f;
    private bool isPlaying = false;

    // Audio Emitters
    private AudioSource sfxSource;
    private AudioSource musicSource;

    public float MusicVolume => audioData != null ? audioData.GlobalMusicVolume : 0.7f;
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
            if (musicSource.isPlaying) StopMusic();
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
        bankMap = new Dictionary<string, AudioData.SoundBank>();
        if (audioData?.SoundBanks == null) return;

        foreach (var bank in audioData.SoundBanks)
            if (!string.IsNullOrEmpty(bank.Name))
                bankMap[bank.Name] = bank;
    }
    #endregion

    #region Sound Effects API
    /// <summary>Plays a random clip from the named sound bank.</summary>
    public void PlaySound(string bankName)
    {
        if (!bankMap.TryGetValue(bankName, out AudioData.SoundBank bank))
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
    public void SetMusicVolume(float volume)
    {
        audioData.GlobalMusicVolume = Mathf.Clamp01(volume);
        musicSource.volume = audioData.GlobalMusicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void StopMusic()
    {
        isPlaying = false;
        musicSource.Stop();
    }
        
    public void SkipTrack() => PlayNextTrack();
    public void PreviousTrack() => PlayPreviousTrack();

    private void PlayPreviousTrack()
    {
        if (trackHistory.Count < 2)
        {
            SkipTrack();
            return;
        }

        PlayTrack(trackHistory.Pop());
    }
    private void PlayNextTrack()
    {
        var playlist = audioData.Playlist;

        if (trackHistory.Count >= 2)
            trackHistory.Push(currentTrackIndex);

        PlayTrack(audioData.ShuffleMusic ? Random.Range(0, playlist.Length) : currentTrackIndex);
    }
    private void PlayTrack(int index)
    {
        if (!IsMusicIDValid(index)) return;

        currentTrackIndex = index;
        AudioData.MusicTrack track = audioData.Playlist[currentTrackIndex];
        musicSource.clip = track.Clip;
        musicSource.volume = track.Volume * audioData.GlobalMusicVolume; // blend per-track + global
        musicSource.Play();
        isPlaying = true;
        EventDispatcher.Broadcast(Events.OnAudioChanged, new("AudioName", track.Title));
    }
    #endregion

    #region Helpers
    public string GetCurrentTrackName()
    {
        if (!IsMusicIDValid(currentTrackIndex))
            return noTrackPlayingString;

        return audioData.Playlist[currentTrackIndex].Title;
    }

    private bool IsMusicIDValid(int id)
    {
        if (audioData != null && // AudioData must be assigned
            audioData.Playlist != null && // Playlist must be assigned
            audioData.Playlist.Length > 0 && // Playlist must have at least one track
            id >= 0 && id < audioData.Playlist.Length) // ID must be within bounds
            return true;

        Log.Warn($"Invalid track index {id}");
        return false;
    }
    #endregion
}
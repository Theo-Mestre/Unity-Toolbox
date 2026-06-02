using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioData", menuName = "Audio/Audio Data")]
public class AudioData : ScriptableObject
{
    [Serializable]
    public class SoundBank
    {
        public string Name;
        public AudioClip[] Clips;
        [Range(0f, 1f)] public float Volume = 1f;
        public bool applyRandomPitch = false;
        [Range(0.8f, 1.2f)] public float PitchVariation = 1f;
    }

    [Serializable]
    public class MusicTrack
    {
        public string Title;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 0.5f;
    }

    // Sounds
    public SoundBank[] SoundBanks;

    // Music
    public MusicTrack[] Playlist;
    [Range(0f, 1f)] public float GlobalMusicVolume = 0.5f;
    public bool AutoStartMusic = true;
    public bool ShuffleMusic = false;
}
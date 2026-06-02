using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioData", menuName = "Audio/Audio Data")]
public class AudioData : ScriptableObject
{
    [Serializable]
    public class SoundBank
    {
        public string Name = "SoundBank";
        public AudioClip[] Clips = null;
        [Range(0f, 1f)] public float Volume = 1f;
        public bool applyRandomPitch = false;
        [Range(0.8f, 1.2f)] public float PitchVariation = 1f;
    }

    [Serializable]
    public class MusicTrack
    {
        public string Title = "MusicTrack";
        public AudioClip Clip = null;
        [Range(0f, 1f)] public float Volume = 1.0f;
    }

    [Serializable]
    public class MusicBank
    {
        public string Name = "MusicBank";
        public MusicTrack[] Tracks = null;
    }

    // Sounds
    public SoundBank[] SoundBanks = null;

    // Music
    public MusicBank[] MusicBanks = null;
    [Range(0f, 1f)] public float GlobalMusicVolume = 1.0f;
    public bool AutoStartMusic = true;
    public bool ShuffleMusic = false;
}
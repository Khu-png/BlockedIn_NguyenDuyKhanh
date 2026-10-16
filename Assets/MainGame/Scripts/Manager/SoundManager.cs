using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    public const string GameplayMusic = "Gameplay";
    public const string ButtonSound = "Button";
    public const string DropSound = "BlockDrop";
    public const string MatchSound = "BlockMatch";
    public const string BreakSound = "BlockBreak";
    public const string IceCrackSound = "IceCrack";
    public const string IceBreakSound = "IceBreak";
    public const string FrozenSound = "FrozenBlock";
    public const string LockedSound = "LockedBlock";
    public const string WinSound = "Win";
    public const string LoseSound = "Lose";
    public const string RopeShowSound = "RopeShow";
    public const string RopeCutSound = "RopeCut";
    public const string RopeLockedSound = "RopeLocked";
    [System.Serializable]
    private struct MusicEntry
    {
        public string id;
        public AudioClip clip;
    }

    [System.Serializable]
    private struct SoundEntry
    {
        public string id;
        public AudioClip clip;
    }

    private const string MUSIC_KEY = "MusicEnabled";
    private const string SOUND_KEY = "SoundEnabled";

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundSource;
    private readonly Dictionary<string, AudioClip> musicLookup = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> soundLookup = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastSoundTimes = new Dictionary<string, float>();

    [SerializeField] private MusicEntry[] musicEntries = new MusicEntry[0];
    [SerializeField] private SoundEntry[] soundEntries = new SoundEntry[0];

    private bool isMusicEnabled = true;
    private bool isSoundEnabled = true;
    private bool isLoaded;
    private bool isMusicSuspended;

    public bool IsMusicEnabled => isMusicEnabled;
    public bool IsSoundEnabled => isSoundEnabled;

    void Awake()
    {
        RegisterSingleton(this);
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.Stop();
        }
        if (soundSource != null)
        {
            soundSource.playOnAwake = false;
            soundSource.loop = false;
            soundSource.spatialBlend = 0f;
            soundSource.Stop();
        }
        LoadSettings();
        BuildClipLookup();
        isLoaded = true;
    }

    void OnValidate()
    {
        BuildClipLookup();
    }

    public void PlayMusic(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !isLoaded || !musicLookup.TryGetValue(id, out AudioClip clip) || clip == null) return;
        if (musicSource == null) return;
        if (musicSource.clip != clip)
        {
            musicSource.Stop();
            musicSource.clip = clip;
        }
        UpdateMusicPlayback();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void SetMusicSuspended(bool suspended)
    {
        isMusicSuspended = suspended;
        UpdateMusicPlayback();
    }

    public void PlaySound(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !isLoaded || !isSoundEnabled) return;
        if (!soundLookup.TryGetValue(id, out AudioClip clip) || clip == null) return;

        if (soundSource == null) return;
        float now = Time.unscaledTime;
        if (lastSoundTimes.TryGetValue(id, out float last) && now - last < .08f) return;
        lastSoundTimes[id] = now;
        soundSource.PlayOneShot(clip);
    }

    public void PlayEffect(AudioClip clip, float volume = 1f)
    {
        if (!isLoaded || !isSoundEnabled || clip == null || soundSource == null) return;
        soundSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void PlayButtonSound(string id = ButtonSound)
    {
        if (!isLoaded || !isSoundEnabled) return;

        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        PlaySound(id);
    }

    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;
        PlayerPrefs.SetInt(MUSIC_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        UpdateMusicPlayback();
    }

    public void SetSoundEnabled(bool enabled)
    {
        isSoundEnabled = enabled;
        PlayerPrefs.SetInt(SOUND_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        if (!enabled && soundSource != null) soundSource.Stop();
    }

    public void ToggleMusic()
    {
        SetMusicEnabled(!isMusicEnabled);
    }

    public void ToggleSound()
    {
        SetSoundEnabled(!isSoundEnabled);
    }

    private void LoadSettings()
    {
        isMusicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
        isSoundEnabled = PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
        UpdateMusicPlayback();
        if (!isSoundEnabled && soundSource != null) soundSource.Stop();
    }

    void UpdateMusicPlayback()
    {
        if (musicSource == null) return;
        if (!isMusicEnabled || isMusicSuspended) { musicSource.Pause(); return; }
        if (musicSource.clip == null || musicSource.isPlaying) return;
        musicSource.UnPause();
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void ReloadSettings()
    {
        lastSoundTimes.Clear();
        if (soundSource != null) soundSource.Stop();
        LoadSettings();
    }

    public static void Play(string id)
    {
        if (Ins != null) Ins.PlaySound(id);
    }

    private void BuildClipLookup()
    {
        musicLookup.Clear();
        soundLookup.Clear();
        foreach (MusicEntry entry in musicEntries ?? System.Array.Empty<MusicEntry>())
        {
            if (string.IsNullOrWhiteSpace(entry.id)) continue;
            if (!musicLookup.TryAdd(entry.id, entry.clip))
                Debug.LogWarning($"Duplicate music ID: {entry.id}. Keeping the first entry.", this);
        }
        foreach (SoundEntry entry in soundEntries ?? System.Array.Empty<SoundEntry>())
        {
            if (string.IsNullOrWhiteSpace(entry.id)) continue;
            if (!soundLookup.TryAdd(entry.id, entry.clip))
                Debug.LogWarning($"Duplicate sound ID: {entry.id}. Keeping the first entry.", this);
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//--------------------------------------------------------------------
//One place for all audio: the background music track and every one-shot sound effect.
//
//Everything is looked up by enum rather than by clip reference, so the callers (ability
//modules, cats, enemies, the game manager) never hold an AudioClip - they just say
//SoundManager.Play(SoundId.Jump) and this decides what that means. Placeholder friendly:
//an id with no entry, or an entry with no clip yet, is a silent no-op, and so is every call
//made when there is no SoundManager in the scene at all (the demo scenes, for instance).
//
//Setup: drop Assets/Prefabs/Managers/SoundManager.prefab into the scene and fill in clips as
//they get made. The two AudioSources build themselves in Awake, so there is nothing to wire.
//--------------------------------------------------------------------
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Sound Effects")]
    [Tooltip("One entry per sound. Ids with no entry, or entries with no clip, stay silent")]
    [SerializeField] List<SoundEntry> m_Sounds = new List<SoundEntry>();

    [Header("Music")]
    [Tooltip("One entry per track. The game manager switches these as the game state changes")]
    [SerializeField] List<MusicEntry> m_Music = new List<MusicEntry>();
    [Tooltip("Seconds to fade out the old track and back in on the new one")]
    [SerializeField] float m_MusicFadeTime = 0.75f;

    [Header("Volume")]
    [Range(0.0f, 1.0f)][SerializeField] float m_MusicVolume = 0.6f;
    [Range(0.0f, 1.0f)][SerializeField] float m_SfxVolume = 1.0f;

    [Header("Debug")]
    [Tooltip("Logs the first time a sound is asked for that has no clip yet. Handy while the audio is still placeholders")]
    [SerializeField] bool m_WarnOnMissingSound = false;

    //Two sounds of the same id closer together than this are almost always a double fire
    //(two colliders, two frames of the same landing) rather than something worth hearing twice
    const float c_MinRepeatInterval = 0.05f;

    AudioSource m_MusicSource;
    AudioSource m_SfxSource;
    Dictionary<SoundId, SoundEntry> m_SoundLookup;
    Dictionary<MusicTrack, MusicEntry> m_MusicLookup;
    Dictionary<SoundId, float> m_LastPlayTimes;
    HashSet<SoundId> m_WarnedSounds;
    MusicTrack m_CurrentTrack = MusicTrack.None;
    Coroutine m_MusicFade;
    bool m_MusicPaused;

    //Gives the component a row for every id the moment it is added, so setup is dragging
    //clips into a ready-made list instead of building it by hand
    void Reset()
    {
        AddMissingEntries();
    }

    //Run this again from the component's context menu after new ids are added to the enums
    [ContextMenu("Add Missing Entries")]
    void AddMissingEntries()
    {
        foreach (SoundId id in System.Enum.GetValues(typeof(SoundId)))
        {
            if (id == SoundId.None || m_Sounds.Exists(entry => entry != null && entry.m_Id == id))
            {
                continue;
            }
            m_Sounds.Add(new SoundEntry { m_Id = id });
        }
        foreach (MusicTrack track in System.Enum.GetValues(typeof(MusicTrack)))
        {
            if (track == MusicTrack.None || m_Music.Exists(entry => entry != null && entry.m_Track == track))
            {
                continue;
            }
            m_Music.Add(new MusicEntry { m_Track = track });
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        m_MusicSource = CreateSource("Music Source", true);
        m_SfxSource = CreateSource("SFX Source", false);

        m_SoundLookup = new Dictionary<SoundId, SoundEntry>();
        for (int i = 0; i < m_Sounds.Count; i++)
        {
            //A duplicate id would throw; the first entry wins and the rest are ignored
            if (m_Sounds[i] != null && !m_SoundLookup.ContainsKey(m_Sounds[i].m_Id))
            {
                m_SoundLookup.Add(m_Sounds[i].m_Id, m_Sounds[i]);
            }
        }
        m_MusicLookup = new Dictionary<MusicTrack, MusicEntry>();
        for (int i = 0; i < m_Music.Count; i++)
        {
            if (m_Music[i] != null && !m_MusicLookup.ContainsKey(m_Music[i].m_Track))
            {
                m_MusicLookup.Add(m_Music[i].m_Track, m_Music[i]);
            }
        }
        m_LastPlayTimes = new Dictionary<SoundId, float>();
        m_WarnedSounds = new HashSet<SoundId>();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    //Built in code so the prefab is just the component - nothing to mis-wire.
    //Both are 2D: this is a side-on game where everything worth hearing is on screen anyway
    AudioSource CreateSource(string a_Name, bool a_Loop)
    {
        GameObject sourceObject = new GameObject(a_Name);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = a_Loop;
        source.spatialBlend = 0.0f;
        return source;
    }

    //--- Sound effects -------------------------------------------------

    //Safe to call from anywhere, including scenes with no SoundManager
    public static void Play(SoundId a_Id)
    {
        if (Instance != null)
        {
            Instance.PlaySound(a_Id);
        }
    }

    public void PlaySound(SoundId a_Id)
    {
        if (a_Id == SoundId.None)
        {
            return;
        }

        SoundEntry entry;
        if (!m_SoundLookup.TryGetValue(a_Id, out entry) || entry.m_Clip == null)
        {
            WarnMissing(a_Id);
            return;
        }

        //Unscaled: sounds still play while the game is paused (menu clicks, mainly)
        float now = Time.unscaledTime;
        float lastPlayed;
        if (m_LastPlayTimes.TryGetValue(a_Id, out lastPlayed) && now - lastPlayed < c_MinRepeatInterval)
        {
            return;
        }
        m_LastPlayTimes[a_Id] = now;

        //A little pitch wobble keeps repeated sounds (footsteps, tosses) from grating
        m_SfxSource.pitch = 1.0f + Random.Range(-entry.m_PitchVariation, entry.m_PitchVariation);
        m_SfxSource.PlayOneShot(entry.m_Clip, entry.m_Volume * m_SfxVolume);
    }

    void WarnMissing(SoundId a_Id)
    {
        if (!m_WarnOnMissingSound || m_WarnedSounds.Contains(a_Id))
        {
            return;
        }
        m_WarnedSounds.Add(a_Id);
        Debug.Log("SoundManager: no clip for " + a_Id + " yet", this);
    }

    //--- Music ---------------------------------------------------------

    public static void PlayMusic(MusicTrack a_Track)
    {
        if (Instance != null)
        {
            Instance.StartMusic(a_Track);
        }
    }

    public void StartMusic(MusicTrack a_Track)
    {
        //Already on this track: let it keep playing rather than restarting it. This is what
        //makes it safe to call from every state change, including ones that repeat
        if (a_Track == m_CurrentTrack)
        {
            return;
        }
        m_CurrentTrack = a_Track;

        MusicEntry entry;
        if (!m_MusicLookup.TryGetValue(a_Track, out entry))
        {
            entry = null;
        }
        AudioClip clip = (entry != null) ? entry.m_Clip : null;
        float volume = ((entry != null) ? entry.m_Volume : 1.0f) * m_MusicVolume;

        if (m_MusicFade != null)
        {
            StopCoroutine(m_MusicFade);
        }
        m_MusicFade = StartCoroutine(FadeToTrack(clip, volume));
    }

    public static void StopMusic()
    {
        PlayMusic(MusicTrack.None);
    }

    //Cuts the music where it stands (the pause menu). Resuming picks the track back up from
    //the same spot rather than restarting it
    public static void PauseMusic()
    {
        if (Instance != null)
        {
            Instance.SetMusicPaused(true);
        }
    }

    public static void ResumeMusic()
    {
        if (Instance != null)
        {
            Instance.SetMusicPaused(false);
        }
    }

    public void SetMusicPaused(bool a_Paused)
    {
        if (a_Paused == m_MusicPaused)
        {
            return;
        }
        m_MusicPaused = a_Paused;

        if (a_Paused)
        {
            m_MusicSource.Pause();
        }
        else if (m_MusicSource.clip != null)
        {
            //UnPause, not Play: Play would start the track over from the beginning
            m_MusicSource.UnPause();
        }
    }

    public bool IsMusicPaused()
    {
        return m_MusicPaused;
    }

    //Fades on unscaled time so a paused game (timeScale 0) doesn't freeze the fade halfway
    IEnumerator FadeToTrack(AudioClip a_Clip, float a_TargetVolume)
    {
        if (m_MusicSource.isPlaying && m_MusicFadeTime > 0.0f)
        {
            float startVolume = m_MusicSource.volume;
            for (float t = 0.0f; t < m_MusicFadeTime; t += Time.unscaledDeltaTime)
            {
                //A fade caught by the pause menu holds where it is and carries on afterwards
                while (m_MusicPaused)
                {
                    yield return null;
                }
                m_MusicSource.volume = Mathf.Lerp(startVolume, 0.0f, t / m_MusicFadeTime);
                yield return null;
            }
        }

        m_MusicSource.Stop();
        m_MusicSource.clip = a_Clip;
        m_MusicSource.volume = 0.0f;

        //No clip for this track yet (or MusicTrack.None): silence is the whole result
        if (a_Clip == null)
        {
            m_MusicFade = null;
            yield break;
        }

        m_MusicSource.Play();
        //Track switched while the game is paused (win screen, say): queue it up silently and
        //let the resume start it, since Play() on its own would ignore the pause
        if (m_MusicPaused)
        {
            m_MusicSource.Pause();
        }
        if (m_MusicFadeTime > 0.0f)
        {
            for (float t = 0.0f; t < m_MusicFadeTime; t += Time.unscaledDeltaTime)
            {
                while (m_MusicPaused)
                {
                    yield return null;
                }
                m_MusicSource.volume = Mathf.Lerp(0.0f, a_TargetVolume, t / m_MusicFadeTime);
                yield return null;
            }
        }
        m_MusicSource.volume = a_TargetVolume;
        m_MusicFade = null;
    }

    //--- Volume --------------------------------------------------------

    //For an options menu later; both take 0-1
    public void SetMusicVolume(float a_Volume)
    {
        m_MusicVolume = Mathf.Clamp01(a_Volume);
        //Only touch the source directly when no fade is running, or the fade would fight it
        if (m_MusicFade == null)
        {
            MusicEntry entry;
            float trackVolume = m_MusicLookup.TryGetValue(m_CurrentTrack, out entry) ? entry.m_Volume : 1.0f;
            m_MusicSource.volume = trackVolume * m_MusicVolume;
        }
    }

    public void SetSfxVolume(float a_Volume)
    {
        m_SfxVolume = Mathf.Clamp01(a_Volume);
    }

    public float GetMusicVolume()
    {
        return m_MusicVolume;
    }

    public float GetSfxVolume()
    {
        return m_SfxVolume;
    }
}

//Every sound the game can make. Add to this list as sounds get made; anything without a clip
//assigned on the SoundManager simply stays quiet
public enum SoundId
{
    None,
    Jump,
    Land,
    PlayerHurt,
    PlayerDeath,
    LevelComplete,
    UIClick,
    //New ids go on the END: the value is what gets serialized on the SoundManager's entries,
    //so inserting one in the middle would silently re-point every clip after it
    Footstep
}

//Background music tracks, switched by GameManager as the game state changes
public enum MusicTrack
{
    None,
    Menu,
    Level,
    Win
}

[System.Serializable]
public class SoundEntry
{
    public SoundId m_Id = SoundId.None;
    public AudioClip m_Clip = null;
    [Range(0.0f, 1.0f)] public float m_Volume = 1.0f;
    [Tooltip("Random pitch wobble, so repeats don't sound identical. 0 plays the clip exactly as recorded")]
    [Range(0.0f, 0.5f)] public float m_PitchVariation = 0.05f;
}

[System.Serializable]
public class MusicEntry
{
    public MusicTrack m_Track = MusicTrack.None;
    public AudioClip m_Clip = null;
    [Range(0.0f, 1.0f)] public float m_Volume = 1.0f;
}

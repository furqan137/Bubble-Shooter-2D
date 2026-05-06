using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Sounds")]
    public Sound[] sounds;

    public bool mute = false;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        foreach (Sound s in sounds)
        {
            s.audioSource = gameObject.AddComponent<AudioSource>();
            s.audioSource.clip = s.clip;
            s.audioSource.volume = s.volume;
            s.audioSource.pitch = s.pitch;
            s.audioSource.loop = s.loop;
        }
    }

    private void Start()
    {
        PlaySound("background");
    }

    public void PlaySound(string soundName)
    {
        Sound s = Array.Find(sounds, x => x.name == soundName);
        if (s == null || s.audioSource == null) return;
        if (!mute) s.audioSource.Play();
    }

    public void StopSound(string soundName)
    {
        Sound s = Array.Find(sounds, x => x.name == soundName);
        if (s == null || s.audioSource == null) return;
        s.audioSource.Stop();
    }

    public void ToggleMute()
    {
        mute = !mute;
        foreach (Sound s in sounds)
            if (s.audioSource != null)
                s.audioSource.mute = mute;
    }
}

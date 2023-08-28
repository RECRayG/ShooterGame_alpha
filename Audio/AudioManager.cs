using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour {

	public static AudioManager instance;

	public Sound[] sounds;

	public List<Sound> savedSounds;
	public Sound playingSoundPause;

	void Awake ()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		} else
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}

		foreach (Sound s in sounds)
		{
			s.source = gameObject.AddComponent<AudioSource>();
			s.source.clip = s.clip;
			s.source.volume = s.volume;
			s.source.pitch = s.pitch;
			s.source.loop = s.loop;
			s.source.playOnAwake = s.playOnAwake;
		}
	}

	public void Play(string sound, AudioSource objectSource = null)
	{
		Sound s = Array.Find(sounds, item => item.name == sound);

		if(objectSource == null)
		{
            s.source.Play();
        } 
		else
		{
			s.sourceObject = objectSource;
			s.sourceObject.Play();
        }
	}
	public void Stop(string sound, AudioSource objectSource = null)
	{
		Sound s = Array.Find(sounds, item => item.name == sound);
		
        if (objectSource == null)
        {
            s.source.Stop();
        }
        else
        {
            s.sourceObject.Stop();
        }
    }
    public void DisappearSound(string sound, float transitionTime, AudioSource objectSource = null)
    {
        Sound s = Array.Find(sounds, item => item.name == sound);

        if (objectSource == null)
        {
            StartCoroutine(DisappearSource(transitionTime, s.source));
        }
        else
        {
            StartCoroutine(DisappearSource(transitionTime, s.sourceObject));
        }
    }

    public void AppearSound(string sound, float transitionTime, AudioSource objectSource = null)
    {
        Sound s = Array.Find(sounds, item => item.name == sound);

        if (objectSource == null)
        {
            StartCoroutine(AppearSource(transitionTime, s.source, s.volume));
        }
        else
        {
            StartCoroutine(AppearSource(transitionTime, s.sourceObject, s.volume));
        }
    }

    IEnumerator DisappearSource(float transitionTime, AudioSource playingSound)
    {
        float percentage = 0;
        while (playingSound.volume > 0)
        {
            playingSound.volume = Mathf.Lerp(playingSound.volume, 0, percentage);
            percentage += Time.deltaTime / transitionTime;
            yield return null;
        }

        playingSound.Stop();
    }

    IEnumerator AppearSource(float transitionTime, AudioSource playingSound, float volume)
    {
        float percentage = 0;
        while (playingSound.volume <= volume)
        {
            playingSound.volume = Mathf.Lerp(0, volume, percentage);
            percentage += Time.deltaTime / transitionTime;
            yield return null;
        }

        playingSound.Stop();
    }

    public void StopAllSounds()
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].sourceObject == null)
            {
                if (sounds[i].source.isPlaying)
                    sounds[i].source.Stop();
            }
            else
            {
                if (sounds[i].sourceObject.isPlaying)
                    sounds[i].sourceObject.Stop();
            }
        }
    }

    public void PauseAllSounds(float transitionTime, string SoundShouldPlay)
	{
        transitionTime = 0;
        Sound s = Array.Find(sounds, item => item.name == SoundShouldPlay);
        playingSoundPause = s;

        for (int i = 0; i < sounds.Length; i++)
		{
            savedSounds.Add(sounds[i]);

            if (sounds[i].sourceObject == null)
            {
                if (sounds[i].source.isPlaying)
                    StartCoroutine(FadeInSource(transitionTime, sounds[i]));
            }
            else
            {
                if (sounds[i].sourceObject.isPlaying)
                    StartCoroutine(FadeInSourceObject(transitionTime, sounds[i]));
            }
        }

        if (!s.source.isPlaying)
        {
            s.source.Play();
        }

        float percentage = 0;

        while (s.source.volume < s.volume)
        {
            s.source.volume = Mathf.Lerp(0, s.volume, percentage);
            percentage = Time.deltaTime / transitionTime;
        }
    }

    public void UnPauseAllSounds(float transitionTime)
    {
        transitionTime = 0;
        for (int i = 0; i < sounds.Length; i++)
        {
            savedSounds.Remove(sounds[i]);

            if (sounds[i].sourceObject == null)
            {
                //if (sounds[i].source.isPlaying)
                    StartCoroutine(FadeOutSource(transitionTime, sounds[i]));
            }
            else
            {
                //if (sounds[i].sourceObject.isPlaying)
                    StartCoroutine(FadeOutSourceObject(transitionTime, sounds[i]));
            }
        }

        if (playingSoundPause.source.isPlaying)
        {
            playingSoundPause.source.Stop();
        }
    }

    IEnumerator FadeInSource(float transitionTime, Sound playingSound)
	{
		float percentage = 0;
		while(playingSound.source.volume > 0)
		{
			playingSound.source.volume = Mathf.Lerp(playingSound.volume, 0, 0);
			//percentage += Time.deltaTime / transitionTime;
			yield return null;
		}

        playingSound.source.Pause();
    }

    IEnumerator FadeInSourceObject(float transitionTime, Sound playingSound)
    {
        float percentage = 0;
        while (playingSound.sourceObject.volume > 0)
        {
            playingSound.sourceObject.volume = Mathf.Lerp(playingSound.volume, 0, 0);
            //percentage += Time.deltaTime / transitionTime;
            yield return null;
        }

        playingSound.sourceObject.Pause();
    }

    IEnumerator FadeOutSource(float transitionTime, Sound playingSound)
    {
        /*if (!playingSound.source.isPlaying)
        {
            playingSound.source.Play();
        }*/
        playingSound.source.UnPause();

        float percentage = 0;
        while (playingSound.source.volume < playingSound.volume)
        {
            playingSound.source.volume = Mathf.Lerp(0, playingSound.volume, 0);
            //percentage += Time.deltaTime / transitionTime;
            yield return null;
        }
    }

    IEnumerator FadeOutSourceObject(float transitionTime, Sound playingSound)
    {
        /*if (!playingSound.sourceObject.isPlaying)
        {
            playingSound.sourceObject.Play();
        }*/
        playingSound.sourceObject.UnPause();

        float percentage = 0;
        while (playingSound.sourceObject.volume < playingSound.volume)
        {
            playingSound.sourceObject.volume = Mathf.Lerp(0, playingSound.volume, 0);
            //percentage += Time.deltaTime / transitionTime;
            yield return null;
        }
    }
}

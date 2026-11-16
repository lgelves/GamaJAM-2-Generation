using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// 1. Catálogo de sonidos. ¡Añade los nombres de los sonidos que quieras aquí!
public enum SoundType
{
    // UI
    UIClick,
    UIHover,
    // Sistema
    PerspectiveChange,
    // Jugador
    PlayerJump,
    PlayerLand,
    PlayerHurt,
    // Enemigos
    ChaserAlert
}

// 2. Estructura para poder arrastrar los audios en el Inspector de Unity
[Serializable]
public class SoundEntry
{
    public SoundType type;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f; // Ajuste de volumen individual por si un clip es muy ruidoso
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer mainMixer;

    [Header("Audio Sources (Channels)")]
    public AudioSource bgmSource;
    public AudioSource sfxPlayerSource; // Canal que interrumpe acciones anteriores del jugador
    public AudioSource sfxSystemSource; // Canal para UI y sistema (se sobreponen)

    [Header("BGM Settings")]
    public AudioClip mainMenuBGM;
    public AudioClip gameplayBGM;
    public float fadeDuration = 1f; // Tiempo de transición entre canciones

    [Header("Sound Library")]
    public List<SoundEntry> soundLibrary = new List<SoundEntry>();
    private Dictionary<SoundType, SoundEntry> soundDictionary;

    private void Awake()
    {
        // Como estamos en una arquitectura de escena única (Single-Scene), el Singleton es muy sencillo
        if (Instance == null)
        {
            Instance = this;
            InitializeDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionary()
    {
        soundDictionary = new Dictionary<SoundType, SoundEntry>();
        foreach (var entry in soundLibrary)
        {
            if (!soundDictionary.ContainsKey(entry.type))
            {
                soundDictionary.Add(entry.type, entry);
            }
        }
    }

    // --- BGM (MÚSICA DE FONDO) ---
    public void PlayMainMenuBGM() => PlayBGM(mainMenuBGM);
    public void PlayGameplayBGM() => PlayBGM(gameplayBGM);

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return; // Evita reiniciar si ya suena la misma

        StartCoroutine(FadeBGM(clip));
    }

    private IEnumerator FadeBGM(AudioClip newClip)
    {
        // 1. Fade Out (Bajar volumen suavemente)
        float time = 0;
        float startVol = bgmSource.volume;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, time / fadeDuration);
            yield return null;
        }

        // 2. Cambiar pista
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 3. Fade In (Subir volumen suavemente)
        time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }
        bgmSource.volume = 1f;
    }

    // --- SFX (EFECTOS DE SONIDO) ---
    public void PlayPlayerSFX(SoundType type)
    {
        if (soundDictionary.TryGetValue(type, out SoundEntry entry) && entry.clip != null)
        {
            // Stop() interrumpe el sonido anterior (ej: recibir daño corta el sonido de salto)
            sfxPlayerSource.Stop();
            sfxPlayerSource.clip = entry.clip;
            sfxPlayerSource.volume = entry.volume;
            sfxPlayerSource.Play();
        }
    }

    public void PlaySystemSFX(SoundType type)
    {
        if (soundDictionary.TryGetValue(type, out SoundEntry entry) && entry.clip != null)
        {
            // PlayOneShot permite reproducir sonidos simultáneos sin cortarse (ej: múltiples clics en la UI)
            sfxSystemSource.PlayOneShot(entry.clip, entry.volume);
        }
    }

    // --- CONTROL DE VOLUMEN DESDE EL MENÚ DE CONFIGURACIÓN ---
    // Convierte el valor lineal del Slider de UI (0.0001 a 1) a Decibelios Logarítmicos (-80 a 0)
    public void SetMasterVolume(float sliderValue)
    {
        float dbValue = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        mainMixer.SetFloat("MasterVol", dbValue);
    }

    public void SetBGMVolume(float sliderValue)
    {
        float dbValue = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        mainMixer.SetFloat("BGMVol", dbValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float dbValue = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        mainMixer.SetFloat("SFXVol", dbValue);
    }
}

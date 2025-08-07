using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("A játék fõ Audio Mixer-e.")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Graphics")]
    [Tooltip("Az elérhetõ felbontások listája.")]
    private Resolution[] resolutions;

    public static SettingsManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Biztosítja, hogy a menedzser ne törlõdjön jelenetváltáskor
        }

        // Felbontások betöltése
        resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();
    }

    void Start()
    {
        // Játék indításakor töltsük be az elmentett beállításokat
        LoadSettings();
    }

    public void LoadSettings()
    {
        // Audio
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 0.75f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 0.75f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 0.75f));

        // Grafika
        SetQuality(PlayerPrefs.GetInt("QualityIndex", 2)); // Alapértelmezett: "Medium"
        SetFullscreen(PlayerPrefs.GetInt("IsFullscreen", 1) == 1);
        int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", GetDefaultResolutionIndex());
        SetResolution(resolutionIndex);

        // FPS
        SetFPSLimit(PlayerPrefs.GetInt("FPSLimit", 60));
    }

    // --- AUDIO ---
    public void SetMasterVolume(float sliderValue)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("MusicVolume", sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("SFXVolume", sliderValue);
    }

    // --- GRAFIKA ---
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityIndex", qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutions.Length == 0) return;
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public Resolution[] GetResolutions() => resolutions;

    private int GetDefaultResolutionIndex()
    {
        Resolution currentResolution = Screen.currentResolution;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == currentResolution.width && resolutions[i].height == currentResolution.height)
            {
                return i;
            }
        }
        return resolutions.Length - 1; // Visszaadja a legmagasabbat, ha nem találja
    }

    // --- FPS ---
    public void SetFPSLimit(int fps)
    {
        Application.targetFrameRate = fps;
        PlayerPrefs.SetInt("FPSLimit", fps);
    }
}

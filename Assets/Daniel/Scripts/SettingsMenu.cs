using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Sliders de Volumen")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // 1. Conectamos los sliders a las funciones del SoundManager
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(SoundManager.Instance.SetMasterVolume);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);

        // 2. Valores por defecto (mitad de volumen al iniciar)
        // En el futuro, aquí puedes cargar los valores guardados con PlayerPrefs
        if (masterSlider != null) masterSlider.value = 0.75f;
        if (bgmSlider != null) bgmSlider.value = 0.75f;
        if (sfxSlider != null) sfxSlider.value = 0.75f;
    }
}

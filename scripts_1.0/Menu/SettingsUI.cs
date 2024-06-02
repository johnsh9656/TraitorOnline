using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles settings in main menu
public class SettingsUI : MonoBehaviour
{
    [SerializeField] LobbyUI lobby;
    [SerializeField] TMP_Text m_volumeText;
    [SerializeField] TMP_Text a_volumeText;
    [SerializeField] Button lowButton;
    [SerializeField] Button mediumButton;
    [SerializeField] Button highButton;
    [SerializeField] Slider m_slider;
    [SerializeField] Slider a_slider;

    [SerializeField] AudioClip buttonSound;
    [SerializeField] AudioClip sliderSound;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bonfireSource;

    void Start()
    {
        m_slider.value = SupremeManager.Instance.GetMasterVolume() * 100;
        a_slider.value = SupremeManager.Instance.GetAmbientVolume() * 100;

        m_slider.onValueChanged.AddListener(UpdateMasterVolume);
        a_slider.onValueChanged.AddListener(UpdateAmbientVolume);

        m_volumeText.text = ((int)(SupremeManager.Instance.GetMasterVolume()*100)).ToString();
        a_volumeText.text = ((int)(SupremeManager.Instance.GetAmbientVolume()*100)).ToString();

        sfxSource.volume = SupremeManager.Instance.GetMasterVolume();
        bonfireSource.volume = SupremeManager.Instance.GetAmbientVolume();

        lowButton.onClick.AddListener(() =>
        {
            lobby.PlaySfx(buttonSound);
            SupremeManager.Instance.SetGraphics(0);
        });
        mediumButton.onClick.AddListener(() =>
        {
            lobby.PlaySfx(buttonSound);
            SupremeManager.Instance.SetGraphics(1);
        });
        highButton.onClick.AddListener(() =>
        {
            lobby.PlaySfx(buttonSound);
            SupremeManager.Instance.SetGraphics(2);
        });
    }

    private void UpdateMasterVolume(float value)
    {
        // value is integer [0,100]
        m_volumeText.text = ((int)value).ToString();
        SupremeManager.Instance.SetMasterVolume(value / 100f);
        sfxSource.volume = SupremeManager.Instance.GetMasterVolume();
        //sfxSource.PlayOneShot(sliderSound);
    }
    private void UpdateAmbientVolume(float value)
    {
        // value is integer [0,100]
        a_volumeText.text = ((int)value).ToString();
        SupremeManager.Instance.SetAmbientVolume(value / 100f);
        bonfireSource.volume = SupremeManager.Instance.GetAmbientVolume();
        //bonfireSource.PlayOneShot(sliderSound);
    }
}

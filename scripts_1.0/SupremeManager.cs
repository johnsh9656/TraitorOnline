using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

// this is the above-all singleton which will persist from the MAIN MENU
// stores player preferences, settings, etc.
public class SupremeManager : NetworkBehaviour
{
    public static SupremeManager Instance { get; private set; }

    private const string PLAYER_PREFS_PLAYER_NAME = "PLAYER NAME";
    private const string PLAYER_PREFS_MASTER_VOLUME = "MASTER VOLUME";
    private const string PLAYER_PREFS_AMBIENT_VOLUME = "AMBIENT VOLUME";
    private const string PLAYER_PREFS_GRAPHICS = "GRAPHICS";

    private string playerName;
    private float masterVolume;
    private float ambientVolume;
    private int graphics;

    [SerializeField] RenderPipelineAsset[] qualityLevels;

    private void Awake()
    {
        if (Instance) Destroy(gameObject);

        Instance = this;

        DontDestroyOnLoad(gameObject);

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME, "PlayerName" + Random.Range(1, 1000));
        masterVolume = PlayerPrefs.GetFloat(PLAYER_PREFS_MASTER_VOLUME, 0.5f);
        ambientVolume = PlayerPrefs.GetFloat(PLAYER_PREFS_AMBIENT_VOLUME, 0.25f);
        graphics = PlayerPrefs.GetInt(PLAYER_PREFS_GRAPHICS, 2);
        QualitySettings.SetQualityLevel(graphics);
        QualitySettings.renderPipeline = qualityLevels[graphics];
    }

    public string GetPlayerName() { return playerName; }
    public float GetMasterVolume() { return masterVolume; }
    public float GetAmbientVolume() { return ambientVolume; }
    public int GetGraphics() { return graphics; }

    public void SetPlayerName(string name) 
    { 
        playerName = name;
        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME, playerName);
    }
    public void SetMasterVolume(float vol) 
    { 
        masterVolume = vol;
        PlayerPrefs.SetFloat(PLAYER_PREFS_MASTER_VOLUME, masterVolume);
    }
    public void SetAmbientVolume(float vol) 
    { 
        ambientVolume = vol;
        PlayerPrefs.SetFloat(PLAYER_PREFS_AMBIENT_VOLUME, ambientVolume);
    }
    public void SetGraphics(int val) 
    { 
        graphics = val;
        PlayerPrefs.SetInt(PLAYER_PREFS_GRAPHICS, graphics);
        QualitySettings.SetQualityLevel(graphics);
        QualitySettings.renderPipeline = qualityLevels[graphics];
    }
}

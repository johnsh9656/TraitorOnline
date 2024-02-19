using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// this is the above-all singleton which will start from the MAIN MENU (not yet implemented)
// currently only store's player's name, but will you used more for more player stats,
// character customization, settings, etc.
public class SupremeManager : NetworkBehaviour
{
    public static SupremeManager Instance { get; private set; }

    private const string PLAYER_PREFS_PLAYER_NAME = "PLAYER NAME";

    private string playerName;

    private void Awake()
    {
        if (Instance) Destroy(gameObject);

        Instance = this;

        DontDestroyOnLoad(gameObject);

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME, "PlayerName" + Random.Range(1, 1000));
    }

    public string GetPlayerName() { return playerName; }

    public void SetPlayerName(string name) 
    { 
        playerName = name;
        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME, playerName);
    }
}

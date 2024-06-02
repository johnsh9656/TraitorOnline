using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Main Lobby UI")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;

    [Header("Settings")]
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private SettingsUI settings;

    [Header("Create Lobby UI")]
    [SerializeField] GameObject createLobbyUI;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button createButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private TextMeshProUGUI maxPlayersText;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip click1;
    [SerializeField] private AudioClip click2;

    [Header("Other")]
    [SerializeField] private LobbyMessageUI lobbyMessage;
    public int maxPlayers;
    private string lobbyName;

    private void Awake()
    {
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
        settingsButton.onClick.AddListener(() =>
        {
            PlaySfx(click1);
            if (settingsUI.activeSelf) settingsUI.SetActive(false);
            else settingsUI.SetActive(true);
        });
        createLobbyButton.onClick.AddListener(() =>
        {
            PlaySfx(click2);
            createLobbyUI.SetActive(true);
            //GameLobby.Instance.CreateLobby(lobbyName, maxPlayers, false);
        });
        playerNameInputField.onValueChanged.AddListener((string newText) =>
        {
            SupremeManager.Instance.SetPlayerName(newText);
        });

        createButton.onClick.AddListener(() =>
        {
            PlaySfx(click1);
            createButton.enabled = false;

            try
            {
                lobbyMessage.ShowMessage("Creating lobby...");

                Debug.Log("lobby name: " + lobbyName);
                // default name if no name given
                if (lobbyName.Replace(" ", "") == "" || lobbyName == null) lobbyName = "Awesomesauce Lobby";

                GameLobby.Instance.CreateLobby(lobbyName, maxPlayers, false);
            }
            catch
            {
                lobbyMessage.ShowMessage("Failed to create lobby.");
                createButton.enabled = true;
            }
            
            
        });
        closeButton.onClick.AddListener(() =>
        {
            PlaySfx(click1);
            createButton.enabled = true;
            createLobbyUI.SetActive(false);
        });
        maxPlayersSlider.onValueChanged.AddListener((float newValue) =>
        {
            PlaySfx(click1);
            maxPlayers = (int)maxPlayersSlider.value;
            maxPlayersText.text = "Max Players: " + maxPlayers.ToString();
        });
        lobbyNameInputField.onValueChanged.AddListener((string newText) =>
        {
            lobbyName = newText;
        });

        lobbyTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        createLobbyUI.SetActive(false);
        playerNameInputField.text = SupremeManager.Instance.GetPlayerName();
        maxPlayers = (int)maxPlayersSlider.value;
        maxPlayersText.text = "Max Players: " + maxPlayers.ToString();

        GameLobby.Instance.OnLobbyListChanged += GameLobby_OnLobbyListChanged;
        UpdateLobbyList(new List<Lobby>());
    }

    private void GameLobby_OnLobbyListChanged(object sender, GameLobby.OnLobbyListChangedEventArgs e)
    {
        UpdateLobbyList(e.lobbyList);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        Debug.Log("Updating lobby list");

        foreach (Transform child in lobbyContainer)
        {
            if (child == lobbyTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            Transform lobbyTransform = Instantiate(lobbyTemplate, lobbyContainer);
            lobbyTransform.gameObject.SetActive(true);
            lobbyTransform.GetComponent<LobbyListSingleUI>().SetLobby(lobby);
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        sfxSource.clip = clip;
        sfxSource.PlayOneShot(clip, sfxSource.volume);
    }

    public void JoinLobbyAudio()
    {
        PlaySfx(click2);
    }

    private void OnDestroy()
    {
        GameLobby.Instance.OnLobbyListChanged -= GameLobby_OnLobbyListChanged;
    }
}

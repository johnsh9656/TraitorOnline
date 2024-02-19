using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    // main lobby UI
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;

    // create lobby UI
    [SerializeField] GameObject createLobbyUI;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button createButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private TextMeshProUGUI maxPlayersText;

    public int maxPlayers;
    private string lobbyName;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.MainMenu);
        });
        createLobbyButton.onClick.AddListener(() =>
        {
            createLobbyUI.SetActive(true);
            //GameLobby.Instance.CreateLobby(lobbyName, maxPlayers, false);
        });
        playerNameInputField.onValueChanged.AddListener((string newText) =>
        {
            SupremeManager.Instance.SetPlayerName(newText);
        });

        createButton.onClick.AddListener(() =>
        {
            GameLobby.Instance.CreateLobby(lobbyName, maxPlayers, false);
        });
        closeButton.onClick.AddListener(() =>
        {
            createLobbyUI.SetActive(false);
        });
        maxPlayersSlider.onValueChanged.AddListener((float newValue) =>
        {
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
    private void OnDestroy()
    {
        GameLobby.Instance.OnLobbyListChanged -= GameLobby_OnLobbyListChanged;
    }
}

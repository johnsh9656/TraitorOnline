using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

// this class is attached to and manages each lobby in the lobby list
// the object has a button component to join the lobby
public class LobbyListSingleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    private Lobby lobby;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            FindObjectOfType<LobbyUI>().JoinLobbyAudio();
            button.enabled = false;
            FindObjectOfType<LobbyMessageUI>().ShowMessage("Joining Lobby...");
            StartCoroutine(JoinLobby());
        });
    }

    private IEnumerator JoinLobby()
    {
        yield return new WaitForSeconds(.5f);
        GameLobby.Instance.JoinWithId(lobby.Id);
    }

    public void SetLobby(Lobby lobby)
    {
        this.lobby = lobby;
        lobbyNameText.text = lobby.Name;
    }
}

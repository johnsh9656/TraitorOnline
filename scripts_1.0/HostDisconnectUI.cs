using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HostDisconnectUI : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject background;

    void Start()
    {
        menuButton.onClick.AddListener(() =>
        {
            MenuButtonPressed();
        });
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        Hide();
    }

    private void MenuButtonPressed()
    {
        Loader.Load(Loader.Scene.Lobby);
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log("disconnect callback");
        if (clientId == NetworkManager.ServerClientId) // server is shutting down
        {
            Show();
        }
    }

    public void Show()
    {
        background.SetActive(true);
    }

    private void Hide()
    {
        background.SetActive(false);
    }
}

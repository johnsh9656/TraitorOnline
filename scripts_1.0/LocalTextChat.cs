using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UIElements;

public class LocalTextChat : NetworkBehaviour
{
    [SerializeField] private GameObject chatPrefab;
    [SerializeField] private GameObject bubble;
    [SerializeField] private TMP_InputField textInput;

    private PlayerAnimHandler animHandler;
    private bool ownedByClient = false;
    private bool typing = false;
    string messageText = "";

    public void SetAsOwned()
    {
        ownedByClient = true;
        textInput.onValueChanged.AddListener(UpdateTextInput);
        animHandler = GetComponentInParent<PlayerAnimHandler>();
    }

    private void Update()
    {
        if (!ownedByClient) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!typing)
            {
                OpenChat();
            } else
            {
                SendMessage();
            }
        }
    }

    private void UpdateTextInput(string newText)
    {
        messageText = newText;
    }

    private void OpenChat()
    {
        typing = true;
        bubble.SetActive(true);
        textInput.text = "";
        textInput.Select();
        animHandler.SetTyping(true);
    }

    private void CloseChat()
    {
        typing = false;
        bubble.SetActive(false);
        animHandler.SetTyping(false);
    }

    private void SendMessage()
    {
        Debug.Log("attempting to send message");
        // if invalid input
        if (messageText.Replace(" ", "") == "" || messageText == null)
        {
            Debug.Log("Invalid message");
            CloseChat();
            return;
        }

        CloseChat();
        SendMessageServerRpc(messageText);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendMessageServerRpc(string message)
    {
        SendMessageClientRpc(message);
    }

    [ClientRpc]
    void SendMessageClientRpc(string message)
    {
        Debug.Log("message sent in chat: " + message);
        GameObject chat = Instantiate(chatPrefab, transform);
        chat.GetComponentInChildren<TMP_Text>().text = message;
        Destroy(chat, 8);
    }
}

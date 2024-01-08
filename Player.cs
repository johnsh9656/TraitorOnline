using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameObject gm;

    [SerializeField] private GameObject hostDisconnectUI;
    [SerializeField] private GameObject playerUICanvas;
    [SerializeField] private GameObject localDeadUI;
    [SerializeField] private GameObject killUI;
    [SerializeField] private GameObject voteUI;
    [SerializeField] public GameObject selectedKillUI;
    [SerializeField] public GameObject selectedVoteUI;
    [SerializeField] private GameObject deadUI;

    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI voteText;
    [SerializeField] private TextMeshProUGUI playerNameTextUI;

    [SerializeField] private Button killButton;
    [SerializeField] private Button voteButton;

    [SerializeField] private Material tempDeadMat;

    private bool isDead = false;
    public bool assigned = false;
    public bool isTraitor = false;
    private bool loadedGameScene = false;

    private NetworkVariable<int> votes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<FixedString128Bytes> networkPlayerName =
        new NetworkVariable<FixedString128Bytes>("Player: 0", NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {
        // set name - TEMPORARY
        // update to match player's name in SupremeManager
        networkPlayerName.Value = "Player: " + (OwnerClientId + 1);
        playerName.text = networkPlayerName.Value.ToString();

        if (OwnerClientId == NetworkManager.LocalClientId)
        {
            Instantiate(hostDisconnectUI);
            playerUICanvas.SetActive(true);
            playerNameTextUI.text = networkPlayerName.Value.ToString();
        }

        if (IsHost && IsOwner) // instantiate gameplay manager
        {
            SpawnGameplayManagerServerRpc();
        }

        killButton.onClick.AddListener(() =>
        {
            KillButtonPressed();
        });
        
        voteButton.onClick.AddListener(() =>
        {
            VoteButtonPressed();
        });

        voteText.text = "0";
        votes.OnValueChanged += VoteValueChanged;
    }

    private void VoteValueChanged(int oldValue, int newValue)
    {
        Debug.Log("votes changed from " + oldValue + " to " + newValue);
        voteText.text = newValue.ToString();
    }

    private void GameSceneStart()
    {
        loadedGameScene = true;
        Vector3 spawnPoint = FindObjectOfType<SpawnPoint>().transform.position + new Vector3(1.5f * OwnerClientId, 0, 0);
        transform.position = spawnPoint;
    }

    private void Update()
    {
        if (!loadedGameScene)
        {
            if (SceneManager.GetActiveScene().name == "GameplayTest") GameSceneStart();
        }

        // assign the gameplay manager once it has been instantiated on the network
        if (!assigned && IsOwner)
        {
            if (FindObjectOfType<GameplayManager>())
            {
                FindObjectOfType<GameplayManager>().AssignLocalPlayer(this);
                assigned = true;
            }
        }
    }

    public void SetVoteUI(bool active)
    {
        voteUI.SetActive(active);
        voteButton.gameObject.SetActive(active);
        selectedVoteUI.SetActive(false);
    }

    public void DisableVoteButton()
    {
        voteButton.enabled = false;
    }

    private void VoteButtonPressed()
    {
        FindObjectOfType<GameplayManager>().SelectVoteTarget(OwnerClientId);
        selectedVoteUI.SetActive(true);
    }

    public void SetKillUI(bool active)
    {
        killUI.SetActive(active);
        selectedKillUI.SetActive(false);
    }

    private void KillButtonPressed()
    {
        FindObjectOfType<GameplayManager>().SelectKillTarget(OwnerClientId);
        selectedKillUI.SetActive(true);
    }

    [ServerRpc]
    private void SpawnGameplayManagerServerRpc()
    {
        GameObject go = Instantiate(gm, Vector3.zero, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }

    public bool IsDead() { return isDead; }

    [ClientRpc]
    public void KillClientRpc() 
    {
        if (IsDead()) return;

        if (NetworkManager.LocalClientId == OwnerClientId) {
            localDeadUI.SetActive(true);
        }

        isDead = true;

        // kill anim
        Debug.Log(GetPlayerName() + " just died");
        GetComponent<MeshRenderer>().material = tempDeadMat;

    }

    public GameObject GetKillButton() { return killButton.gameObject; }

    [ServerRpc(RequireOwnership = false)]
    public void AddVoteServerRpc()
    {
        votes.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveVoteServerRpc()
    {
        votes.Value--;
    }

    [ServerRpc]
    public void ClearAllVotesServerRpc()
    {
        votes.Value = 0;
    }

    public string GetPlayerName()
    {
        return networkPlayerName.Value.ToString();
    }

    public int GetVotes()
    {
        return votes.Value;
    }
}

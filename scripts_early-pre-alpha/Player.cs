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

    private bool isDead = false;
    public bool assigned = false;
    public bool isTraitor = false;
    private bool loadedGameScene = false;

    private NetworkVariable<int> votes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<FixedString128Bytes> networkPlayerName =
        new NetworkVariable<FixedString128Bytes>("null", NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
    private LocalTextChat textChat;
    public SkinManager skinManager;
    private PlayerAnimHandler animHandler;

    public override void OnNetworkSpawn()
    {
        textChat = GetComponentInChildren<LocalTextChat>();
        skinManager = GetComponentInChildren<SkinManager>();
        animHandler = GetComponent<PlayerAnimHandler>();
        networkPlayerName.OnValueChanged += NameValueChanged;

        if (OwnerClientId == NetworkManager.LocalClientId)
        {
            //SetPlayerNameServerRpc(SupremeManager.Instance.GetPlayerName());
            networkPlayerName.Value = SupremeManager.Instance.GetPlayerName();
            Instantiate(hostDisconnectUI);
            animHandler.enabled = true;
            animHandler.SetAsLocalPlayer();
            playerUICanvas.SetActive(true);
            textChat.SetAsOwned();
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

        playerName.text = networkPlayerName.Value.ToString();
        playerNameTextUI.text = networkPlayerName.Value.ToString();
    }

    private void NameValueChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        playerName.text = networkPlayerName.Value.ToString();
        playerNameTextUI.text = networkPlayerName.Value.ToString();
    }

    private void VoteValueChanged(int oldValue, int newValue)
    {
        Debug.Log("votes changed from " + oldValue + " to " + newValue);
        voteText.text = newValue.ToString();
    }

    private void GameSceneStart()
    {
        loadedGameScene = true;
        Transform spawnPoint = FindObjectOfType<SpawnPoint>().GetSpawnpoint((int)OwnerClientId);
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
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

    public void EnableVoteButton()
    {
        voteButton.enabled = true;
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
            textChat.enabled = false;
        }

        isDead = true;

        // kill anim
        Debug.Log(GetPlayerName() + " just died");

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

    [ServerRpc(RequireOwnership = false)]
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

    public void ResetEmoteIndex()
    {
        animHandler.ResetEmoteIndexEvent();
    }
}

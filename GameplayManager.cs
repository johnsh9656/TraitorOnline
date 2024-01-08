using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;
using Unity.Collections;
using System.Linq;
using Unity.Services.Authentication;

public class GameplayManager : NetworkBehaviour
{
    const int MIN_PLAYERS = 2;

    [SerializeField] GameObject descriptionUI;
    [SerializeField] GameObject votingUI;
    [SerializeField] GameObject endUI;

    [SerializeField] Button hostStartButton;
    [SerializeField] Button leaveLobbyButton;
    [SerializeField] Button readyButton;
    [SerializeField] Button confirmKillButton;
    [SerializeField] Button confirmVoteButton;
    [SerializeField] Button skipVoteButton;

    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI playerCountText;
    [SerializeField] TextMeshProUGUI playersReadyText;
    [SerializeField] TextMeshProUGUI roleText;
    [SerializeField] TextMeshProUGUI littleRoleText;
    [SerializeField] TextMeshProUGUI votingText;
    [SerializeField] TextMeshProUGUI skipVoteText;
    [SerializeField] TextMeshProUGUI dayText;

    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> readyPlayers = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    
    // -1 game not started ; 0 assigning roles ; 1 day; 2 night
    private NetworkVariable<int> phase = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone);

    private NetworkVariable<int> day = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> skipVote = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<ulong> killTargetID = new NetworkVariable<ulong>();
    private NetworkVariable<FixedString128Bytes> tempPlayerName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone);
    
    private List<Player> innocents = new List<Player>();
    private List<Player> allPlayers = new List<Player>();
    private Player localPlayer;

    private int voteTarget = -1;
    private int innocentsLeftToWin = 0; // temp -- should be 2 //
    private ulong traitorId = 0;

    private void Awake()
    {
        hostStartButton.onClick.AddListener(() =>
        {
            HostStartButtonPressed();
        });
        
        leaveLobbyButton.onClick.AddListener(() =>
        {
            if (IsServer) HostLeaveClientRpc();

            NetworkManager.Singleton.Shutdown();
            LeaveLobbyButtonPressedServerRpc(AuthenticationService.Instance.PlayerId);
            Loader.Load(Loader.Scene.Lobby);
        });

        readyButton.onClick.AddListener(() =>
        {
            ReadyButtonPressed();
        });   
        
        confirmKillButton.onClick.AddListener(() =>
        {
            ConfirmKillButtonPressed();
        }); 
        
        skipVoteButton.onClick.AddListener(() =>
        {
            SkipVoteButtonPressed();
        }); 
        
        confirmVoteButton.onClick.AddListener(() =>
        {
            ConfirmVoteButtonPressed();
        });    
    }

    public override void OnNetworkSpawn()
    {
        day.OnValueChanged += DayValueChanged;
        skipVote.OnValueChanged += SkipVoteValueChanged;
        playerCount.OnValueChanged += PlayerCountValueChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        
        if (IsServer) { hostStartButton.gameObject.SetActive(true); Debug.Log("HOST"); }

        Debug.Log("gameplay manager spawned");
    }

    private void OnClientDisconnect(ulong disconnectId)
    {
        if (disconnectId==traitorId) { InnocentsWinClientRpc(); return; }

        Debug.Log("removed " + allPlayers[(int)disconnectId].GetPlayerName());

        // this is already handled automatically by netcode
        /*innocents.Remove(allPlayers[(int)disconnectId]);
        allPlayers.Remove(allPlayers[(int)disconnectId]);*/

        if (GetInnocentsLeft() <= innocentsLeftToWin) { TraitorWinsClientRpc(); }
    }

    private void DayValueChanged(int oldValue, int newValue)
    {
        Debug.Log("The day has updated to" + newValue.ToString());
        dayText.text = "Day: " + newValue.ToString();
    }

    private void SkipVoteValueChanged(int oldValue, int newValue)
    {
        skipVoteText.text = newValue.ToString();
    }

    private void PlayerCountValueChanged(int oldValue, int newValue)
    {
        playerCountText.text = "Players: " + newValue;
    }

    private void Update()
    {
        playersReadyText.text = "Players ready: " + readyPlayers.Value.ToString();

        // temp - later can only be called onclientconnectcallback and onclientdisconnectcallback
        if (IsServer) playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    [ClientRpc]
    private void HostLeaveClientRpc()
    {
        FindObjectOfType<HostDisconnectUI>().Show(); // may be redundant
        LeaveLobbyButtonPressedServerRpc(AuthenticationService.Instance.PlayerId);
    }

    private void HostStartButtonPressed()
    {
        if (playerCount.Value < MIN_PLAYERS) { return; }

        // clean ui + end lobby
        leaveLobbyButton.gameObject.SetActive(false);
        hostStartButton.gameObject.SetActive(false);
        GameLobby.Instance.DeleteLobby();

        gameStarted.Value = true;
        phase.Value = 0;
        AssignRolesPhaseServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void LeaveLobbyButtonPressedServerRpc(string id)
    {
        GameLobby.Instance.KickPlayer(id);
    }

    private void ReadyButtonPressed()
    {
        readyButton.enabled = false;

        ReadyButtonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyButtonServerRpc()
    {
        // check if all players ready
        if (IsServer)
        {
            readyPlayers.Value++;

            if (readyPlayers.Value == GetInnocentsLeft()+1)
            {
                readyPlayers.Value = 0;
                readyButton.gameObject.SetActive(false);

                // determine day or night depending on phase value
                if (phase.Value == 0)
                {
                    phase.Value = 1;
                    DayPhaseClientRpc();
                }
                else if (phase.Value == 1)
                {
                    phase.Value = 2;
                    NightPhaseClientRpc();
                }
            }
        }
    }

    [ClientRpc]
    private void InitializeReadyButtonClientRpc()
    {
        if (IsServer) readyPlayers.Value = 0;

        readyButton.gameObject.SetActive(true);
        playersReadyText.gameObject.SetActive(true);
        readyButton.enabled = true;
    }

    [ServerRpc]
    private void AssignRolesPhaseServerRpc()
    {
        int rand = Random.Range(0, NetworkManager.Singleton.ConnectedClients.Count);
        traitorId = (ulong) rand;
        AssignRolesPhaseClientRpc(traitorId);
    }

    [ClientRpc]
    private void AssignRolesPhaseClientRpc(ulong t_id)
    {
        leaveLobbyButton.gameObject.SetActive(false);

        traitorId = t_id;
        readyButton.gameObject.SetActive(false);
        roleText.gameObject.SetActive(true);

        // assign playeres to allPlayers
        foreach (Player p in FindObjectsOfType<Player>())
        {
            allPlayers.Add(p);
            if (!p.IsDead() && p.NetworkObject.OwnerClientId != traitorId) innocents.Add(p);
        }
        Debug.Log("innocents count: " + innocents.Count);

        allPlayers[(int)traitorId].isTraitor = true;

        // sort the allPlayers list
        allPlayers = allPlayers.OrderBy(p => p.OwnerClientId).ToList();

        if (NetworkManager.LocalClientId == traitorId)
        {
            roleText.text = "You are: TRAITOR";
            littleRoleText.text = "Traitor";
        }

        Debug.Log("local client id: " + NetworkManager.LocalClientId);
        Debug.Log("traitor id: " + traitorId.ToString());

        // add wait time, animation
        InitializeReadyButtonClientRpc();
    }

    [ClientRpc]
    private void DayPhaseClientRpc()
    {
        // clear ui
        Debug.Log("day: " + day.Value);
        readyButton.gameObject.SetActive(false);
        roleText.gameObject.SetActive(false);
        littleRoleText.gameObject.SetActive(true);
        dayText.gameObject.SetActive(true);

        // day 1 -- no voting, no shopping
        if (day.Value == 1)
        {
            InitializeReadyButtonClientRpc();
            return;
        }

        // description
        descriptionUI.SetActive(true);
        if ((int)killTargetID.Value != -1)
        {
            descriptionText.text = "In the middle of the night... " + tempPlayerName.Value.ToString() + " was killed.";
        }
        else
        {
            descriptionText.text = "In the middle of hte night... nothing happened.";
        }

        if (IsServer)
        {
            StartCoroutine(WaitToVote());
        }

        if (localPlayer.IsDead())
        {
            return;
        }

        // set visuals to day

        // description - if day one build world

        // voting

        // work, shop, or other activities

        // timer
        // int list for votes for each client + skip option
        // if person voted out - check for end game
        // else move to night phase

        //InitializeReadyButtonClientRpc();
    }

    private IEnumerator WaitToVote()
    {
        yield return new WaitForSecondsRealtime(10f);
        VotingPhaseClientRpc();
    }

    [ClientRpc]
    private void VotingPhaseClientRpc()
    {
        descriptionUI.SetActive(false);
        votingUI.SetActive(true);
        voteTarget = -1; // represents "skip vote"

        if (localPlayer.IsDead())
        {
            confirmVoteButton.gameObject.SetActive(false);
            skipVoteButton.enabled = false;
        }

        foreach (Player p in allPlayers)
        {
            if (p.IsDead()) continue;

            p.SetVoteUI(true);

            if (localPlayer.IsDead()) 
                p.DisableVoteButton();
        }

        if (IsServer)
        {
            readyPlayers.Value = 0;
            skipVote.Value = GetInnocentsLeft() + 1;
        }
    }

    public void SelectVoteTarget(ulong targetId)
    {
        SelectVoteTargetServerRpc(voteTarget, targetId);
        voteTarget = (int)targetId;

        foreach (Player p in allPlayers)
        {
            if (p.IsDead()) continue;
            else if (p.NetworkObject.OwnerClientId == targetId)
            {
                votingText.text = "Voting For: " + p.GetPlayerName();
            } else
            {
                p.selectedVoteUI.SetActive(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectVoteTargetServerRpc(int previousTarget, ulong targetId)
    {
        if (previousTarget != -1) // remove vote from previous target
        {
            NetworkManager.Singleton.ConnectedClients[(ulong)previousTarget].PlayerObject.GetComponent<Player>().RemoveVoteServerRpc();
        }
        else // previous target was skip vote
        {
            skipVote.Value--;
        }

        NetworkManager.Singleton.ConnectedClients[targetId].PlayerObject.GetComponent<Player>().AddVoteServerRpc();
    }

    private void SkipVoteButtonPressed()
    {
        if (voteTarget == -1) return;

        allPlayers[voteTarget].RemoveVoteServerRpc();
        allPlayers[voteTarget].selectedVoteUI.SetActive(false);
        votingText.text = "Voting For: Skip Vote";
        voteTarget = -1;

        SkipVoteButtonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SkipVoteButtonServerRpc()
    {
        skipVote.Value++;
    }

    private void ConfirmVoteButtonPressed()
    {
        confirmVoteButton.enabled = false;
        foreach (Player p in allPlayers)
        {
            p.DisableVoteButton();
        }

        ConfirmVoteButtonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmVoteButtonServerRpc()
    {
        readyPlayers.Value++;

        if (readyPlayers.Value >= GetInnocentsLeft()+1) // end voting
        {
            readyPlayers.Value = 0;

            // determine outcome
            int mostVotes = skipVote.Value;
            List<Player> votedIds = new List<Player>();
            foreach (Player p in allPlayers)
            {
                if (p.IsDead()) continue;

                if (p.GetVotes()>mostVotes)
                {
                    mostVotes = p.GetVotes();
                    votedIds.Clear();
                    votedIds.Add(p);
                }
                else if (p.GetVotes()==mostVotes)
                {
                    votedIds.Add(p);
                }
            }

            if (mostVotes >= MajorityValue() && votedIds.Count == 1) // someone dies
            {
                votedIds[0].KillClientRpc();

                string names = votedIds[0].GetPlayerName() + " was ";
                string description = names + "was killed for treason.";
                EndVotingPhaseClientRpc(description);

            } else // no one dies
            {
                string description = "No one was voted out.";
                EndVotingPhaseClientRpc(description);
            }
        }
    }

    [ClientRpc]
    private void EndVotingPhaseClientRpc(string description)
    {
        ////// add delay //////


        // disable voting ui
        votingUI.SetActive(false);
        foreach (Player p in allPlayers)
        {
            p.SetVoteUI(false);
            p.ClearAllVotesServerRpc();
        }

        // description
        descriptionUI.SetActive(true);
        descriptionText.text = description;

        if (IsServer)
        {
            StartCoroutine(WaitAfterVotingDescription());
        }
    }

    private IEnumerator WaitAfterVotingDescription()
    {
        yield return new WaitForSecondsRealtime(5f);
        ContinueAfterVoteServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ContinueAfterVoteServerRpc()
    {
        Player traitor_p = NetworkManager.Singleton.ConnectedClients[traitorId].PlayerObject.GetComponent<Player>();

        if (traitor_p.IsDead()) // innocents win
        {
            InnocentsWinClientRpc();
        }
        else if (GetInnocentsLeft() > innocentsLeftToWin) // continue to night
        {
            InitializeReadyButtonClientRpc();
        } 
        else // traitor wins
        {
            TraitorWinsClientRpc();
        }
    }

    [ClientRpc]
    private void NightPhaseClientRpc()
    {
        if (localPlayer.IsDead())
        {
            return;
        }

        Debug.Log("night");
        readyButton.gameObject.SetActive(false);
        // set visuals to night

        // activate kill ui of each player
        if (NetworkManager.LocalClientId == traitorId)
        {
            foreach (Player p in innocents)
            {
                Debug.Log(p.name);

                if (p.IsDead()) continue;

                p.SetKillUI(true);
            }        
        }
        // else
            // sleep minigame? (later)
            // mute text chat (later)
            // mute voice chat (later)
    }

    public void SelectKillTarget(ulong targetId)
    {
        confirmKillButton.gameObject.SetActive(true);
        SetKillTargetServerRpc(targetId);

        foreach (Player p in innocents)
        {
            if (p.IsDead()) continue;
            else if (p.NetworkObject.OwnerClientId == targetId)
            {
                // separating target from rest
                Debug.Log(p.name);
            } else
            {
                p.selectedKillUI.SetActive(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetKillTargetServerRpc(ulong targetId)
    {
        if (IsServer)
        {
            killTargetID.Value = targetId;
        }
    }

    private void ConfirmKillButtonPressed()
    {
        // assume successful

        confirmKillButton.gameObject.SetActive(false);

        foreach (Player p in innocents)
        {
            p.selectedKillUI.SetActive(false);
            p.SetKillUI(false);
        }

        ConfirmKillButtonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmKillButtonServerRpc()
    {
        NetworkManager.Singleton.ConnectedClients[killTargetID.Value].PlayerObject.GetComponent<Player>().KillClientRpc();
        tempPlayerName.Value = NetworkManager.Singleton.ConnectedClients[killTargetID.Value].PlayerObject.GetComponent<Player>().GetPlayerName();

        if (GetInnocentsLeft() > innocentsLeftToWin)
        {
            // continue to next day
            day.Value++;
            phase.Value = 1;
            StartCoroutine(WaitBeforeNextDay());
        } else
        {
            // end game - Traitor wins
            TraitorWinsClientRpc();
        }
    }

    // the purpose of this is to allow for the networkvariables to change their value
    // on all clients before their new values are used in the next day
    private IEnumerator WaitBeforeNextDay()
    {
        yield return new WaitForSeconds(4);
        DayPhaseClientRpc();
    }

    public int GetInnocentsLeft()
    {
        if (innocents.Count==0) {  return (playerCount.Value-1); }

        int count = 0;
        foreach (Player p in innocents)
        {
            if (p.IsDead()) continue;
            count++;
        }
        return count;
    }

    [ClientRpc]
    private void TraitorWinsClientRpc()
    {
        Destroy(FindObjectOfType<HostDisconnectUI>());
        descriptionUI.SetActive(false);

        EndUI eui = Instantiate(endUI).GetComponentInChildren<EndUI>();
        eui.Set("The Traitor Wins", allPlayers[(int)traitorId].GetPlayerName());

        NetworkManager.Singleton.Shutdown();
        Destroy(FindObjectOfType<HostDisconnectUI>());
    }

    [ClientRpc]
    private void InnocentsWinClientRpc()
    {
        Destroy(FindObjectOfType<HostDisconnectUI>());
        descriptionUI.SetActive(false);

        EndUI eui = Instantiate(endUI).GetComponentInChildren<EndUI>();
        eui.Set("The Innocents Win", allPlayers[(int)traitorId].GetPlayerName());

        NetworkManager.Singleton.Shutdown();
        Destroy(FindObjectOfType<HostDisconnectUI>());
    }

    public void AssignLocalPlayer(Player p)
    {
        localPlayer = p;
    }

    private int MajorityValue()
    {
        int count = GetInnocentsLeft() + 1;
        if (count % 2 == 0) return count / 2;
        else return (int) (count / 2) + 1;
    }
}

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
    const float PHASE_LENGTH = 30f;

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
    [SerializeField] TextMeshProUGUI phaseText;

    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> readyPlayers = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    
    private NetworkVariable<int> phase = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone);

    private NetworkVariable<int> day = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> skipVote = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<ulong> killTargetID = new NetworkVariable<ulong>(100);
    private NetworkVariable<FixedString128Bytes> tempPlayerName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone);
    
    private List<Player> innocents = new List<Player>();
    private List<Player> allPlayers = new List<Player>();
    private Player localPlayer;

    private int voteTarget = -1;
    private int innocentsLeftToWin = 0; // temp -- should be 2 //
    private ulong traitorId = 0;

    private DayNightCycle dayNightCycle;
    private Timer timer;
    private bool gameOver = false;

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
            //ReadyButtonPressed();
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
        phase.OnValueChanged += PhaseValueChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        
        if (IsServer) { hostStartButton.gameObject.SetActive(true); Debug.Log("HOST"); }

        Debug.Log("gameplay manager spawned");
    }

    private void OnClientDisconnect(ulong disconnectId)
    {
        if (gameOver) return;

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

    private void PhaseValueChanged(int oldValue, int newValue)
    {
        string phase = "";
        if (newValue == 0)
        {
            phase = "Night";
        } else if (newValue == 1)
        {
            phase = "Vote";
        } else if (newValue == 2)
        {
            phase = "Day";
        }
        phaseText.text = phase;
        Debug.Log("Phase changed to " + phase);
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
        LeaveLobbyButtonPressedServerRpc(AuthenticationService.Instance.PlayerId);
        if (gameOver) return;

        FindObjectOfType<HostDisconnectUI>().Show(); // may be redundant
    }

    private void HostStartButtonPressed()
    {
        if (playerCount.Value < MIN_PLAYERS) { return; }

        // clean ui + end lobby
        leaveLobbyButton.gameObject.SetActive(false);
        hostStartButton.gameObject.SetActive(false);
        GameLobby.Instance.DeleteLobby();

        gameStarted.Value = true;
        AssignRolesPhaseServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void LeaveLobbyButtonPressedServerRpc(string id)
    {
        GameLobby.Instance.KickPlayer(id);
    }

    /*private void ReadyButtonPressed()
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
                timer.ResetTimerClientRpc();
                readyPlayers.Value = 0;
                readyButton.gameObject.SetActive(false);

                if (phase.Value == 0) // from night to voting
                {
                    phase.Value = 1;
                    VotePhaseClientRpc();
                }
                else if (phase.Value == 1) // from voting to day
                {
                    phase.Value = 2;
                    DayPhaseClientRpc();
                }
                else if (phase.Value == 2) // from day to night
                {
                    phase.Value = 0;
                    NightPhaseClientRpc();
                }
            }
        }
    }*/

    public void TimerEnded()
    {
        Debug.Log("Timer ended!\nPhase: " + phase.Value);
        readyPlayers.Value = 0;

        if (phase.Value == 1)
        {
            phase.Value = 2;
            TimeEndVoteClientRpc();
        }
        else if (phase.Value == 2)
        {
            phase.Value = 0;
            StartCoroutine(WaitToTransitionDayNight());
        }
        else if (phase.Value == 0)
        {
            phase.Value = 1;
            TimeEndNightPhaseClientRpc();
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

        // assign players to allPlayers
        foreach (Player p in FindObjectsOfType<Player>())
        {
            allPlayers.Add(p);
            if (!p.IsDead() && p.NetworkObject.OwnerClientId != traitorId) innocents.Add(p);

            // not optimal but works temporarily
            p.skinManager.Hide();
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

        timer = FindObjectOfType<Timer>();
        timer.SetGameplayManager(this);
        dayNightCycle = FindObjectOfType<DayNightCycle>();

        // add wait time, animation
        if (IsServer) StartCoroutine(WaitToStartGame());
    }

    private IEnumerator WaitToStartGame()
    {
        yield return new WaitForSecondsRealtime(4f);
        DayPhaseClientRpc();
    }

    [ClientRpc]
    private void VotePhaseDescriptionClientRpc()
    {
        Debug.Log("day: " + day.Value);

        // description
        descriptionUI.SetActive(true);
        if ((int)killTargetID.Value != 100)
        {
            descriptionText.text = "In the middle of the night... " + tempPlayerName.Value.ToString() + " was killed.";
        }
        else
        {
            descriptionText.text = "It was a calm night.";
        }

        if (IsServer)
        {
            phase.Value = 1;
            StartCoroutine(WaitToVote());
        }

        /*if (localPlayer.IsDead())
        {
            return;
        }*/
    }

    private IEnumerator WaitToVote()
    {
        yield return new WaitForSecondsRealtime(4f);
        VotingPhaseClientRpc();
    }

    [ClientRpc]
    private void VotingPhaseClientRpc()
    {
        Debug.Log("MADE IT TO VOTING RPC");
        // set visuals and timer for voting
        dayNightCycle.SetState(2, PHASE_LENGTH, false);
        timer.SetTimeRemaining(PHASE_LENGTH);

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
        Debug.Log("pressed confirm vote button");

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
            //QuickTransitionToDayClientRpc();

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
    private void QuickTransitionToDayClientRpc()
    {
        dayNightCycle.SetState(0, 2f, true);
        timer.ResetTimer();
    }

    [ClientRpc]
    private void TimeEndVoteClientRpc() // for when timer runs out instead of all players confirming vote
    {
        readyButton.gameObject.SetActive(false);
        playersReadyText.gameObject.SetActive(false);

        // clean up ui
        confirmVoteButton.enabled = false;
        skipVoteButton.enabled = false;
        foreach (Player p in allPlayers)
        {
            p.DisableVoteButton();
        }
        
        if (IsHost)
        {
            TimeEndVoteHostSide();
        }
    }

    private void TimeEndVoteHostSide()
    {
        readyPlayers.Value = 0;

        // determine outcome
        int mostVotes = skipVote.Value;
        List<Player> votedIds = new List<Player>();
        foreach (Player p in allPlayers)
        {
            if (p.IsDead()) continue;

            if (p.GetVotes() > mostVotes)
            {
                mostVotes = p.GetVotes();
                votedIds.Clear();
                votedIds.Add(p);
            }
            else if (p.GetVotes() == mostVotes)
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

        }
        else // no one dies
        {
            string description = "No one was voted out.";
            EndVotingPhaseClientRpc(description);
        }
    }

    [ClientRpc]
    private void EndVotingPhaseClientRpc(string description)
    {
        Debug.Log("Ending Voting Phase!");

        // disable voting ui
        votingUI.SetActive(false);
        foreach (Player p in allPlayers)
        {
            p.SetVoteUI(false);
            p.ClearAllVotesServerRpc();
            p.EnableVoteButton();
        }
        confirmVoteButton.enabled = true;
        skipVoteButton.enabled = true;

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
        yield return new WaitForSecondsRealtime(4f);

        Player traitor_p = NetworkManager.Singleton.ConnectedClients[traitorId].PlayerObject.GetComponent<Player>();

        if (traitor_p.IsDead()) // innocents win
        {
            InnocentsWinClientRpc();
        }
        else if (GetInnocentsLeft() > innocentsLeftToWin) // continue to day
        {
            HandleVisualsVoteToDayClientRpc();
            yield return new WaitForSecondsRealtime(2f);
            DayPhaseClientRpc();
        }
        else // traitor wins
        {
            TraitorWinsClientRpc();
        }
    }

    [ClientRpc]
    private void HandleVisualsVoteToDayClientRpc()
    {
        dayNightCycle.SetState(2, 2f, true);
        timer.ResetTimer();
    }

    /*[ServerRpc(RequireOwnership = false)]
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
    }*/

    [ClientRpc]
    private void DayPhaseClientRpc()
    {
        Debug.Log("ITS DAY TIME");
        // set visuals to day
        dayNightCycle.SetState(0, PHASE_LENGTH, false);
        timer.SetTimeRemaining(PHASE_LENGTH);
        descriptionUI.SetActive(false);

        // day one - description to build world, start day/night cycle
        if (day.Value == 1)
        {
            playersReadyText.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(false);
            roleText.gameObject.SetActive(false);

            littleRoleText.gameObject.SetActive(true);
            dayText.gameObject.SetActive(true);
            return;
        }

        if (IsServer) phase.Value = 2;

        // work, shop, or other activities

        //InitializeReadyButtonClientRpc();
    }

    private IEnumerator WaitToTransitionDayNight()
    {
        readyButton.gameObject.SetActive(false);
        playersReadyText.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(2f);
        NightPhaseClientRpc();
        //EndDayPhaseClientRpc();
    }

    [ClientRpc]
    private void EndDayPhaseClientRpc()
    {
        Debug.Log("day ending from timer");
        NightPhaseClientRpc();
    }

    [ClientRpc]
    private void NightPhaseClientRpc()
    {
        // set visuals to night
        dayNightCycle.SetState(1, PHASE_LENGTH, false);
        timer.SetTimeRemaining(PHASE_LENGTH);
        if (IsServer) phase.Value = 0;

        if (localPlayer.IsDead())
        {
            return;
        }

        Debug.Log("ITS NIGHT");
        

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
            HandleVisualsNightToDayClientRpc();
            StartCoroutine(WaitBeforeNextDay());
        } else
        {
            // end game - Traitor wins
            TraitorWinsClientRpc();
        }
    }

    [ClientRpc]
    private void HandleVisualsNightToDayClientRpc()
    {
        dayNightCycle.SetState(1, 2f, true);
        timer.ResetTimer();
    }

    [ClientRpc]
    private void TimeEndNightPhaseClientRpc()
    {
        readyButton.gameObject.SetActive(false);
        playersReadyText.gameObject.SetActive(false);

        // disable ui for traitor
        if (NetworkManager.LocalClientId == traitorId)
        {
            confirmKillButton.gameObject.SetActive(false);

            foreach (Player p in innocents)
            {
                p.selectedKillUI.SetActive(false);
                p.SetKillUI(false);
            }
        }

        if (IsHost)
        {
            killTargetID.Value = 100;
            day.Value++;
            StartCoroutine(WaitBeforeNextDay());
        }
    }

    // the purpose of this is to allow for the networkvariables to change their value
    // on all clients before their new values are used in the next day
    private IEnumerator WaitBeforeNextDay()
    {
        yield return new WaitForSeconds(4);
        VotePhaseDescriptionClientRpc();
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
        gameOver = true;
        Destroy(FindObjectOfType<HostDisconnectUI>());
        Destroy(timer);
        Destroy(dayNightCycle);
        descriptionUI.SetActive(false);

        EndUI eui = Instantiate(endUI).GetComponentInChildren<EndUI>();
        eui.Set("The Traitor Wins", allPlayers[(int)traitorId].GetPlayerName());

        Destroy(FindObjectOfType<HostDisconnectUI>());

        if (IsServer) StartCoroutine(WaitDisconnectHost());
    }

    [ClientRpc]
    private void InnocentsWinClientRpc()
    {
        gameOver = true;
        Destroy(FindObjectOfType<HostDisconnectUI>());
        Destroy(timer);
        Destroy(dayNightCycle);
        descriptionUI.SetActive(false);

        EndUI eui = Instantiate(endUI).GetComponentInChildren<EndUI>();
        eui.Set("The Innocents Win", allPlayers[(int)traitorId].GetPlayerName());

        Destroy(FindObjectOfType<HostDisconnectUI>());
        if (IsServer) StartCoroutine(WaitDisconnectHost());
    }

    private IEnumerator WaitDisconnectHost()
    {
        yield return new WaitForSecondsRealtime(2f);
        NetworkManager.Singleton.Shutdown();
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

    public bool GameStarted()
    {
        return gameStarted.Value;
    }
}

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
    const int MIN_PLAYERS = 3;
    const float PHASE_LENGTH = 60f;


    [Header("General")]
    [SerializeField] GameObject endUI;
    [SerializeField] TextMeshProUGUI playerCountText;
    [SerializeField] TextMeshProUGUI roleText;
    [SerializeField] TextMeshProUGUI littleRoleText;
    [SerializeField] TextMeshProUGUI dayText;
    [SerializeField] TextMeshProUGUI phaseText;
    private List<Player> innocents = new List<Player>();
    private List<Player> allPlayers = new List<Player>();
    private Player localPlayer;
    private DayNightCycle dayNightCycle;
    private Timer timer;
    private bool gameOver = false;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeTime;

    [Header("Lobby")]
    [SerializeField] Button hostStartButton;
    [SerializeField] Button leaveLobbyButton;
    [SerializeField] Button readyButton;
    [SerializeField] TextMeshProUGUI playersReadyText;

    [Header("Voting")]
    [SerializeField] GameObject descriptionUI;
    [SerializeField] GameObject votingUI;
    [SerializeField] Button confirmVoteButton;
    [SerializeField] Button skipVoteButton;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI skipVoteText;
    [SerializeField] TextMeshProUGUI votingText;
    private int voteTarget = -1;
    private int innocentsLeftToWin = 1; 
    private float vote_multiplier = 1;

    [Header("Day Phase stuff")]
    [SerializeField] Button workButton;
    [SerializeField] float workCooldown = 5f;
    [SerializeField] Button restButton;
    [SerializeField] float restCooldown = 5f;
    [SerializeField] Button shopButton;
    [SerializeField] Button evilShopButton;
    [SerializeField] GameObject shopUI;
    [SerializeField] GameObject evilShopUI;
    private bool shopEnabled = false;
    private bool evilShopEnabled = false;
    [SerializeField] TextMeshProUGUI restWaitText;
    [SerializeField] TextMeshProUGUI workWaitText;
    private int restCooldownCounter = 5;
    private int workCooldownCounter = 5;
    [SerializeField] Shop shop;
    [SerializeField] Shop evilShop;
    [SerializeField] Minigame[] minigames;
    private Minigame currentMinigame;

    [Header("Night Phase stuff")]
    [SerializeField] GameObject attemptKillUI;
    [SerializeField] TMP_Text attemptKillDefenseText;
    [SerializeField] TMP_Text attemptKillAttackText;
    [SerializeField] TMP_Text attemptKillDefensePercentText;
    [SerializeField] TMP_Text attemptKillAttackPercentText;
    [SerializeField] TMP_Text attemptKillOutcomeText;
    [SerializeField] Button confirmKillButton;
    [SerializeField] Button skipNightButton;
    private ulong traitorId = 0;
    private int attackAttempts = 0;

    // Network Variables
    private NetworkVariable<bool> killSuccessful = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> readyPlayers = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> phase = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> day = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> skipVote = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<ulong> killTargetID = new NetworkVariable<ulong>(100);
    private NetworkVariable<FixedString128Bytes> tempPlayerName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone);
    private NetworkVariable<bool> playerKilled = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);

    [Header("Audio")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource ambientSource;
    [SerializeField] AudioClip playerJoinedSFX;
    [SerializeField] AudioClip startGameSFX;
    [SerializeField] AudioClip click1;
    [SerializeField] public AudioClip changeSkinSFX;
    [SerializeField] public AudioClip timerTickSFX;
    [SerializeField] public AudioClip minigamePointSFX;
    [SerializeField] public AudioClip minigameClickSFX;
    [SerializeField] public AudioClip minigameEndSFX;
    [SerializeField] public AudioClip buyItemSFX;
    [SerializeField] public AudioClip workButtonSFX;
    [SerializeField] public AudioClip restButtonSFX;
    [SerializeField] public AudioClip shopButtonSFX;
    [SerializeField] AudioClip voteButtonSFX;
    [SerializeField] AudioClip confirmVoteButtonSFX;
    [SerializeField] AudioClip playerVotedOutSFX;
    [SerializeField] AudioClip traitorWinsSFX;
    [SerializeField] AudioClip innocentsWinSFX;
    [SerializeField] AudioClip killButtonSFX;
    [SerializeField] AudioClip attemptKillStartSFX;
    [SerializeField] AudioClip attemptKillSuccessSFX;
    [SerializeField] AudioClip attemptKillFailSFX;
    [SerializeField] AudioClip nightStartSFX;
    [SerializeField] AudioClip dayStartSFX;

    private void Awake()
    {
        sfxSource.volume = SupremeManager.Instance.GetMasterVolume();
        ambientSource.volume = SupremeManager.Instance.GetAmbientVolume();

        hostStartButton.onClick.AddListener(() =>
        {
            HostStartButtonPressed();
        });

        leaveLobbyButton.onClick.AddListener(() =>
        {
            if (IsServer) HostLeaveClientRpc();

            PlaySFX(click1);
            NetworkManager.Singleton.Shutdown();
            LeaveLobbyButtonPressedServerRpc(AuthenticationService.Instance.PlayerId);
            Loader.Load(Loader.Scene.Lobby);
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

        workButton.onClick.AddListener(() =>
        {
            WorkButtonPressed();
            PlaySFX(workButtonSFX);
        });

        restButton.onClick.AddListener(() =>
        {
            RestButtonPressed();
            PlaySFX(restButtonSFX);
        });

        shopButton.onClick.AddListener(() =>
        {
            ShopButtonPressed();
            PlaySFX(shopButtonSFX);
        });

        evilShopButton.onClick.AddListener(() =>
        {
            EvilShopButtonPressed();
        });

        skipNightButton.onClick.AddListener(() =>
        {
            SkipNightButtonPressed();
        });
    }

    public override void OnNetworkSpawn()
    {
        day.OnValueChanged += DayValueChanged;
        skipVote.OnValueChanged += SkipVoteValueChanged;
        playerCount.OnValueChanged += PlayerCountValueChanged;
        phase.OnValueChanged += PhaseValueChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;

        if (IsServer) { hostStartButton.gameObject.SetActive(true); Debug.Log("HOST"); }
    }

    private void OnClientConnect(ulong connectId)
    {
        if (!IsHost) return;

        playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;

        PlayerJoinedClientRpc();
    }

    private void OnClientDisconnect(ulong disconnectId)
    {
        if (gameOver) return;

        if (disconnectId == traitorId) { InnocentsWinClientRpc(); return; }

        Debug.Log("removed " + allPlayers[(int)disconnectId].GetPlayerName());

        if (GetInnocentsLeft() <= innocentsLeftToWin) { TraitorWinsClientRpc(); }

        if (IsHost) playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;
    }

    private void DayValueChanged(int oldValue, int newValue)
    {
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

        GameStartFadeClientRpc();
    }
    
    [ClientRpc]
    private void GameStartFadeClientRpc()
    {
        PlaySFX(startGameSFX);
        StartCoroutine(GameStartFade());
    }

    private IEnumerator GameStartFade()
    {
        float time = 0.0f;

        // fade to black
        while (time < fadeTime)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, Mathf.Lerp(0, 1, time / fadeTime));
            time += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1);


        if (IsServer)
        {
            AssignRolesPhaseServerRpc();
            gameStarted.Value = true;
        }
        yield return new WaitForSeconds(2f);

        // fade back
        time = 0.0f;
        while (time < fadeTime)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, Mathf.Lerp(1, 0, time / fadeTime));
            time += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
    }

    [ServerRpc(RequireOwnership = false)]
    private void LeaveLobbyButtonPressedServerRpc(string id)
    {
        GameLobby.Instance.KickPlayer(id);
    }

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
            FadeDayNightClientRpc();
        }
        else if (phase.Value == 0)
        {
            phase.Value = 1;
            TimeEndNightPhaseClientRpc();
        }
    }

    [ServerRpc]
    private void AssignRolesPhaseServerRpc()
    {
        int rand = Random.Range(0, NetworkManager.Singleton.ConnectedClients.Count);
        traitorId = (ulong)rand;
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

        // sort the allPlayers list
        allPlayers = allPlayers.OrderBy(p => p.OwnerClientId).ToList();

        Debug.Log("Player list");
        foreach (Player p in allPlayers)
        {
            Debug.Log(p.OwnerClientId.ToString() + ": " + p.GetPlayerName());
        }

        if (NetworkManager.LocalClientId == traitorId)
        {
            roleText.text = "You are: TRAITOR";
            littleRoleText.text = "Traitor";
            localPlayer.EnableAttackStatUI();
            localPlayer.isTraitor = true;
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
        yield return new WaitForSecondsRealtime(5f);
        DayPhaseClientRpc();
    }

    [ClientRpc]
    private void VotePhaseDescriptionClientRpc()
    {
        Debug.Log("day: " + day.Value);

        // description
        descriptionUI.SetActive(true);
        if (attackAttempts == 0)
        {
            descriptionText.text = "It was an unusually calm night.";
        }
        else if (attackAttempts == 1)
        {
            descriptionText.text = "1 player was attacked last night.";
        }
        else
        {
            descriptionText.text = attackAttempts.ToString() + " attacks were made last night.";
        }

        if (playerKilled.Value)
        {
            descriptionText.text += "\n\n" + tempPlayerName.Value.ToString() + "was killed.";
        }

        if (IsServer)
        {
            phase.Value = 1;
            StartCoroutine(WaitToVote());
        }

        skipNightButton.gameObject.SetActive(false);
    }

    private IEnumerator WaitToVote()
    {
        yield return new WaitForSecondsRealtime(4f);
        VotingPhaseClientRpc();
    }

    [ClientRpc]
    private void VotingPhaseClientRpc()
    {
        // handle temp defense from previous day
        localPlayer.RemoveTempDefense();

        // handle voting ui + reset target
        descriptionUI.SetActive(false);
        votingUI.SetActive(true);
        voteTarget = -1; // represents "skip vote"

        // player death animation if killed
        if (NetworkManager.LocalClientId == killTargetID.Value)
        {
            localPlayer.GetComponent<PlayerAnimHandler>().Die();
        }

        if (localPlayer.IsDead())
        {
            confirmVoteButton.gameObject.SetActive(false);
            skipVoteButton.gameObject.SetActive(false);
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
        for (int i = 0; i < vote_multiplier; i++)
        {
            SelectVoteTargetServerRpc(voteTarget, targetId);
        }
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

        // vote button audio
        PlaySFX(voteButtonSFX);
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

        // handle vote multiplier
        for (int i = 1; i < vote_multiplier; i++)
        {
            SkipVoteButtonServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SkipVoteButtonServerRpc()
    {
        skipVote.Value++;
    }

    private void ConfirmVoteButtonPressed()
    {
        ConfirmVoteSFXClientRpc();

        confirmVoteButton.enabled = false;
        skipVoteButton.enabled = false;
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

        if (readyPlayers.Value >= GetInnocentsLeft() + 1) // end voting
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

                PlayerVotedOutSFXClientRpc();

            } else // no one dies
            {
                string description = "No one was voted out.";
                EndVotingPhaseClientRpc(description);
            }
        }
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

    public void SetVoteMultiplier(int vm)
    {
        vote_multiplier = vm;
    }

    [ClientRpc]
    private void DayPhaseClientRpc()
    {
        // set visuals to day
        dayNightCycle.SetState(0, PHASE_LENGTH, false);
        timer.SetTimeRemaining(PHASE_LENGTH);
        descriptionUI.SetActive(false);

        // day one
        if (day.Value == 1)
        {
            playersReadyText.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(false);
            roleText.gameObject.SetActive(false);

            littleRoleText.gameObject.SetActive(true);
            dayText.gameObject.SetActive(true);
        } else
        {
            PlaySFX(dayStartSFX);
        }

        if (IsServer)
        {
            phase.Value = 2;
            playerKilled.Value = false;
        }

        // task buttons and shops
        shop.SetUpShop(day.Value);
        evilShop.SetUpShop(day.Value);

        workButton.gameObject.SetActive(true);
        workButton.enabled = true;
        restButton.gameObject.SetActive(true);
        restButton.enabled = true;
        shopButton.gameObject.SetActive(true);
        shopButton.enabled = true;

        if (localPlayer.isTraitor)
        {
            evilShopButton.gameObject.SetActive(true);
            evilShopButton.enabled = true;
        }

        // reset vote multiplier
        vote_multiplier = 1;
    }

    private void WorkButtonPressed()
    {
        // disable task buttons and shops
        restButton.gameObject.SetActive(false);
        shopButton.gameObject.SetActive(false);
        evilShopButton.gameObject.SetActive(false);
        workButton.gameObject.SetActive(false);
        workButton.enabled = false;
        HideShop();
        HideEvilShop();

        // select and start minigame
        int index = Random.Range(0, minigames.Length);
        currentMinigame = minigames[index];
        currentMinigame.gameObject.SetActive(true);
        currentMinigame.StartMinigame();

        SetPlayerTaskTextServerRpc(localPlayer.OwnerClientId, "Working...");
    }

    public void MinigameEnded()
    {
        // get payout, add to local player
        localPlayer.ChangeMoney(currentMinigame.GetPayout());

        // close minigame
        currentMinigame.gameObject.SetActive(false);

        // start cooldown
        if (IsDay())
        {
            StartCoroutine(WorkButtonCooldown());
            restButton.gameObject.SetActive(true);
            shopButton.gameObject.SetActive(true);
            workButton.gameObject.SetActive(true);

            if (localPlayer.isTraitor)
            {
                evilShopButton.gameObject.SetActive(true);
            }
        }
        else
        {
            workButton.enabled = true;
        }

        SetPlayerTaskTextServerRpc(localPlayer.OwnerClientId, "");
    }

    private IEnumerator WorkButtonCooldown()
    {
        workWaitText.gameObject.SetActive(true);
        workWaitText.text = ((int)workCooldown).ToString();
        workCooldownCounter = (int)workCooldown;

        for (int i = 0; i < workCooldown; i++)
        {
            yield return new WaitForSeconds(1);
            workCooldownCounter -= 1;
            workWaitText.text = workCooldownCounter.ToString();
        }

        // can work again
        workButton.enabled = true;

        workWaitText.gameObject.SetActive(false);
    }

    private void RestButtonPressed()
    {
        // disable task buttons
        workButton.enabled = false;
        restButton.enabled = false;
        shopButton.enabled = false;
        evilShopButton.enabled = false;

        HideShop();
        SetPlayerTaskTextServerRpc(localPlayer.OwnerClientId, "Resting...");
        restWaitText.gameObject.SetActive(true);
        restCooldownCounter = (int)restCooldown;
        restWaitText.text = restCooldownCounter.ToString();
        StartCoroutine(RestButtonCooldown());
    }

    private IEnumerator RestButtonCooldown()
    {
        for (int i = 0; i < restCooldown; i++)
        {
            yield return new WaitForSeconds(1);
            restCooldownCounter -= 1;
            restWaitText.text = restCooldownCounter.ToString();
        }

        // can task again
        workButton.enabled = true;
        restButton.enabled = true;
        shopButton.enabled = true;
        evilShopButton.enabled = true;
        restWaitText.gameObject.SetActive(false);

        SetPlayerTaskTextServerRpc(localPlayer.OwnerClientId, "");
        localPlayer.ChangeDefense(1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerTaskTextServerRpc(ulong id, string content)
    {
        SetPlayerTaskTextClientRpc(id, content);
    }

    [ClientRpc]
    private void SetPlayerTaskTextClientRpc(ulong id, string content)
    {
        allPlayers[(int)id].SetTaskUI(content);
    }

    private void ShopButtonPressed()
    {
        if (shopEnabled) HideShop();
        else ShowShop();
    }

    private void HideShop()
    {
        shopUI.SetActive(false);
        shopEnabled = false;
    }

    private void ShowShop()
    {
        shopUI.SetActive(true);
        shopEnabled = true;
        HideEvilShop();
    }

    private void EvilShopButtonPressed()
    {
        if (evilShopEnabled) HideEvilShop();
        else ShowEvilShop();
    }

    private void HideEvilShop()
    {
        evilShopUI.SetActive(false);
        evilShopEnabled = false;
    }

    private void ShowEvilShop()
    {
        evilShopUI.SetActive(true);
        evilShopEnabled = true;
        HideShop();
    }

    [ClientRpc]
    private void FadeDayNightClientRpc()
    {
        StartCoroutine(WaitToTransitionDayNight());
    }

    private IEnumerator WaitToTransitionDayNight()
    {
        readyButton.gameObject.SetActive(false);
        playersReadyText.gameObject.SetActive(false);

        float time = 0.0f;

        // fade to black
        while (time < fadeTime)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, Mathf.Lerp(0, 1, time / fadeTime));
            time += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1);

        // set visuals to night
        dayNightCycle.SetState(1, PHASE_LENGTH, false);
        timer.SetTimeRemaining(PHASE_LENGTH);

        NightPhaseClientRpc();
        yield return new WaitForSeconds(2f);

        // fade back
        time = 0.0f;
        while (time < fadeTime)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, Mathf.Lerp(1, 0, time / fadeTime));
            time += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);

    }

    [ClientRpc]
    private void NightPhaseClientRpc()
    {
        // hide day / task ui
        workButton.gameObject.SetActive(false);
        restButton.gameObject.SetActive(false);
        shopButton.gameObject.SetActive(false);
        evilShopButton.gameObject.SetActive(false);
        HideShop();
        HideEvilShop();

        attackAttempts = 0;

        // audio
        PlaySFX(nightStartSFX);

        if (IsServer) phase.Value = 0;

        if (localPlayer.IsDead())
        {
            return;
        }

        if (NetworkManager.LocalClientId == traitorId)
        {
            skipNightButton.gameObject.SetActive(true);

            // activate kill ui of each player
            foreach (Player p in innocents)
            {
                Debug.Log(p.name);

                if (p.IsDead()) continue;

                p.SetKillUI(true);
            }
        }
        // else
    }

    public void SelectKillTarget(Player targetPlayer)
    {
        PlaySFX(killButtonSFX);

        foreach (Player p in innocents)
        {
            p.SetKillUI(false);
        }

        StartCoroutine(WaitToKillAttempt(targetPlayer.OwnerClientId));
    }

    [ServerRpc(RequireOwnership = false)]
    private void AttemptKillServerRpc(ulong targetId)
    {
        //Debug.Log("made it to serverrpc");
        AttemptKillClientRpc(targetId);
    }

    private IEnumerator WaitToKillAttempt(ulong targetId)
    {
        yield return new WaitForSeconds(1f);
        AttemptKillServerRpc(targetId);
    }

    [ClientRpc]
    private void AttemptKillClientRpc(ulong targetId)
    {
        if (IsServer) killTargetID.Value = targetId;

        attackAttempts++;

        // for testing purposes
        Debug.Log("kill target: " + killTargetID.Value.ToString());
        Debug.Log("local player is traitor: " + localPlayer.isTraitor);

        if (localPlayer.isTraitor || targetId == localPlayer.OwnerClientId)
        {
            PlaySFX(attemptKillStartSFX);
        }
        else return;

        // enable ui and determine stats
        attemptKillUI.SetActive(true);
        int defense = allPlayers[(int)targetId].GetDefense();
        int attack = allPlayers[(int)traitorId].GetAttack();
        float attackPercent = (attack / (attack + defense)) * 100f;
        float defensePercent = (defense / (attack + defense)) * 100f;

        // set ui
        attemptKillDefenseText.text = defense.ToString();
        attemptKillAttackText.text = attack.ToString();
        attemptKillAttackPercentText.text = attackPercent.ToString("0.00");
        attemptKillDefensePercentText.text = defensePercent.ToString("0.00");
        attemptKillOutcomeText.text = "";

        if (localPlayer.isTraitor)
        {
            bool killSuccess = Random.Range(0, 100) <= attackPercent;
            SetKillOutcomeBuffer(killSuccess);
        }

        StartCoroutine(AttemptKillWait(defense, attack));
    }

    // calling serverrpc from clientrpc sometimes causes errors
    private void SetKillOutcomeBuffer(bool success)
    {
        SetKillOutcomeServerRpc(success);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetKillOutcomeServerRpc(bool success)
    {
        killSuccessful.Value = success;
    }

    private IEnumerator AttemptKillWait(int defense, int attack)
    {
        string baseText = localPlayer.isTraitor ? "Attempting to kill" : "Attempting to defend";
        attemptKillOutcomeText.text = baseText;

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1f);
            attemptKillOutcomeText.text += ".";
        }
        attemptKillOutcomeText.text = baseText;
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1.5f);
            attemptKillOutcomeText.text += ".";
        }

        if (killSuccessful.Value)
        {
            attemptKillOutcomeText.text = allPlayers[(int)killTargetID.Value].GetPlayerName() + " has been killed.";
            if (localPlayer.isTraitor)
            {
                int attackDif = (int)Mathf.Round(defense / 2) + 1;
                localPlayer.ChangeAttack(-attackDif);
                attemptKillOutcomeText.text += "\nAttack decreased by " + attackDif.ToString();

                ConfirmKillButtonServerRpc();

            } else
            {
                localPlayer.KillClientRpc();
            }
            PlaySFX(attemptKillSuccessSFX);

            yield return new WaitForSeconds(4f);

            attemptKillUI.SetActive(false);
        }
        else
        {
            attemptKillOutcomeText.text = allPlayers[(int)killTargetID.Value].GetPlayerName() + " fought off the Traitor.";
            if (localPlayer.isTraitor)
            {
                int attackDif = 2;
                localPlayer.ChangeAttack(-attackDif);
                attemptKillOutcomeText.text += "\nAttack decreased by " + attackDif.ToString();

                foreach (Player p in innocents)
                {
                    p.SetKillUI(true);
                }

            } else
            {
                localPlayer.ChangeDefense(-attack);
                attemptKillOutcomeText.text += "\nDefense decreased by " + attack.ToString();
            }

            PlaySFX(attemptKillFailSFX);

            yield return new WaitForSeconds(4f);

            attemptKillUI.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetKillTargetServerRpc(ulong targetId)
    {
        killTargetID.Value = targetId;
    }

    private void ConfirmKillButtonPressed()
    {
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
        tempPlayerName.Value = allPlayers[(int)killTargetID.Value].GetPlayerName();
        playerKilled.Value = true;

        if (GetInnocentsLeft() > innocentsLeftToWin)
        {
            // continue to next day
            day.Value++;
            HandleVisualsNightToDayClientRpc();
        } else
        {
            // end game - Traitor wins
            TraitorWinsClientRpc();
        }
    }

    [ClientRpc]
    private void HandleVisualsNightToDayClientRpc()
    {
        skipNightButton.gameObject.SetActive(false);
        StartCoroutine(FadeToDay());
    }

    private IEnumerator FadeToDay()
    {
        float time = 0.0f;

        // fade to black
        while (time < fadeTime)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, Mathf.Lerp(0, 1, time / fadeTime));
            time += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1);

        // set visuals
        dayNightCycle.SetState(2, PHASE_LENGTH, false);
        timer.SetTimeRemaining(PHASE_LENGTH);

        yield return new WaitForSeconds(2f);

        // fade back
        time = 0.0f;
        while (time < fadeTime)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, Mathf.Lerp(1, 0, time / fadeTime));
            time += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);

        VotePhaseDescriptionClientRpc();
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
            SkipNightServerRpc();
        }
    }

    private void SkipNightButtonPressed()
    {
        // continue to next day
        skipNightButton.gameObject.SetActive(false);
        SkipNightServerRpc();

        foreach (Player p in innocents)
        {
            p.selectedKillUI.SetActive(false);
            p.SetKillUI(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SkipNightServerRpc()
    {
        day.Value++;
        HandleVisualsNightToDayClientRpc();
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

        Destroy(FindObjectOfType<HostDisconnectUI>().gameObject);
        if (IsServer) StartCoroutine(WaitDisconnectHost());

        PlaySFX(traitorWinsSFX);
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

        Destroy(FindObjectOfType<HostDisconnectUI>().gameObject);
        if (IsServer) StartCoroutine(WaitDisconnectHost());

        PlaySFX(innocentsWinSFX);
    }

    private IEnumerator WaitDisconnectHost()
    {
        yield return new WaitForSecondsRealtime(10f);
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

    public Player GetLocalPlayer()
    {
        return localPlayer;
    }

    public bool GameStarted()
    {
        return gameStarted.Value;
    }

    public int GetDay()
    {
        return day.Value;
    }

    public bool IsDay()
    {
        return phase.Value == 2;
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.clip = clip;
        sfxSource.PlayOneShot(clip, sfxSource.volume);
    }

    [ClientRpc]
    public void PlayerJoinedClientRpc()
    {
        PlaySFX(playerJoinedSFX);
    }

    [ClientRpc]
    private void ConfirmVoteSFXClientRpc()
    {
        PlaySFX(confirmVoteButtonSFX);
    }

    [ClientRpc]
    private void PlayerVotedOutSFXClientRpc()
    {
        PlaySFX(playerVotedOutSFX);
    }
}

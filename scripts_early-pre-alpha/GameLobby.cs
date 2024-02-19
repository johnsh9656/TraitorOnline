using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

// This script is called for everything related to Unity's Lobby and Relay Services
// and is available in the lobby and game scenes
public class GameLobby : NetworkBehaviour
{
    public static GameLobby Instance { get; private set; }
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private Lobby joinedLobby;
    private float heartbeatTimer = 0f;
    private float listLobbiesTimer = 0f;

    private void Awake()
    {
        // if lobby is not destroyed before leaving the game scene,
        // it must be deleted with the lobby scene
        DeleteLobby();
        joinedLobby = null;

        if (Instance != null) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    public override void OnNetworkSpawn()
    {
        // removes GameLobby object of non-local client
        if (OwnerClientId != NetworkManager.LocalClientId) Destroy(gameObject);
    }

    private void Update()
    {
        HandleHeartBeat();
        HandlePeriodicListLobbies();
    }

    private void HandleHeartBeat()
    {
        // if heartbeat is not sent at least every 30 secs, lobby will be destroyed (Lobby Services)
        if (joinedLobby != null && IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private void HandlePeriodicListLobbies()
    {
        if (joinedLobby == null && AuthenticationService.Instance.IsSignedIn 
            && SceneManager.GetActiveScene().name == Loader.Scene.Lobby.ToString())
        {
            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0f)
            {
                float listLobbiesTimerMax = 3f;
                listLobbiesTimer = listLobbiesTimerMax;
                ListLobbies();
            }
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
            {
                lobbyList = queryResponse.Results
            });
        }
        catch (LobbyServiceException e) { Debug.Log(e); }
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private async Task<Allocation> AllocateRelay(int maxPlayers)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

            return allocation;
        }
        catch (RelayServiceException e) { Debug.Log(e); return default; }
       
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        } catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        } catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            Allocation allocation = await AllocateRelay(maxPlayers);
            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE , new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });
            NetworkManager.Singleton.GetComponent<UnityTransport>().
                SetRelayServerData(new RelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartHost();
            Loader.LoadNetwork(Loader.Scene.GameplayTest);
        } catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
        
    }

    public async void JoinWithId(string lobbyId)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
           
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            NetworkManager.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void DeleteLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                joinedLobby = null;
            } catch (LobbyServiceException e) { Debug.Log(e); }
        }
    }

    // currently not referenced - only works from host-side
    // KickPlayer is used as alternative
    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            joinedLobby = null;
            Loader.Load(Loader.Scene.Lobby);
        }
        catch (LobbyServiceException e) { Debug.Log(e); }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e) { Debug.Log(e); }
        }
    }
}

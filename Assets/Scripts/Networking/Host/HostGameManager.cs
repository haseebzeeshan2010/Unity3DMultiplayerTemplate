using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

public class HostGameManager : IDisposable
{
    private Allocation allocation;
    private string joinCode;
    private string lobbyId;
    public NetworkServer NetworkServer { get; private set; }
    private const int MaxConnections = 20;
    private const string GameSceneName = "Game";
    private Coroutine heartbeatCoroutine;

    /// <summary>
    /// Starts the host by creating a relay allocation, obtaining a join code, creating a lobby, and initializing the network server.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task StartHostAsync()
    {
        // Relay allocation
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay allocation failed: {e}");
            return;
        }

        // Join code
        try
        {
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Join code: {joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get join code: {e}");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null.");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport component not found on NetworkManager.");
            return;
        }

        RelayServerData relayServerData;
        try
        {
            relayServerData = AllocationUtils.ToRelayServerData(allocation, "wss");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create RelayServerData: {ex}");
            return;
        }
        transport.SetRelayServerData(relayServerData);

        // Lobby creation
        string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Host");
        try
        {
            var lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "JoinCode", new DataObject(
                            visibility: DataObject.VisibilityOptions.Member,
                            value: joinCode
                        )
                    }
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(
                $"{playerName}'s Lobby", MaxConnections, lobbyOptions);
            lobbyId = lobby.Id;
            if (HostSingleton.Instance == null)
            {
                Debug.LogError("HostSingleton.Instance is null. Cannot start heartbeat.");
            }
            else
            {
                heartbeatCoroutine = HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15));
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobby creation failed: {e}");
            return;
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error during lobby creation: {e}");
            return;
        }

        // Dispose previous server if exists
        if (NetworkServer != null)
        {
            NetworkServer.Dispose();
        }

        NetworkServer = new NetworkServer(NetworkManager.Singleton);

        string playerId = AuthenticationService.Instance?.PlayerId;
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("PlayerId is null or empty. Authentication may have failed.");
            return;
        }
        UserData userData = new UserData
        {
            username = playerName,
            userAuthId = playerId
        };
        string payload;
        try
        {
            payload = JsonUtility.ToJson(userData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to serialize UserData: {ex}");
            return;
        }
        byte[] payloadBytes;
        try
        {
            payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to encode payload: {ex}");
            return;
        }
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        try
        {
            NetworkManager.Singleton.StartHost();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start host: {ex}");
            return;
        }

        NetworkServer.OnClientLeft += async (authId) => await HandleClientLeft(authId);

        try
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load game scene: {ex}");
        }
    }

    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            var heartbeatTask = LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            while (!heartbeatTask.IsCompleted)
            {
                yield return null;
            }
            if (heartbeatTask.Exception != null)
            {
                Debug.LogException(heartbeatTask.Exception);
            }
            yield return delay;
        }
    }

    /// <summary>
    /// Releases all resources used by the HostGameManager.
    /// </summary>
    public void Dispose()
    {
        Shutdown();
    }

    public async void Shutdown()
    {
        if (heartbeatCoroutine != null && HostSingleton.Instance != null)
        {
            HostSingleton.Instance.StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }

        if (!string.IsNullOrEmpty(lobbyId))
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to delete lobby: {e}");
            }
            lobbyId = string.Empty;
        }

        if (NetworkServer != null)
        {
            NetworkServer.OnClientLeft -= async (authId) => await HandleClientLeft(authId);
            NetworkServer.Dispose();
            NetworkServer = null;
        }
    }

    private async Task HandleClientLeft(string authId)
    {
        if (!string.IsNullOrEmpty(lobbyId))
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to remove player from lobby: {e}");
            }
        }
    }
}


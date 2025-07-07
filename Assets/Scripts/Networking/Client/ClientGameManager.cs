using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;
using Unity.Services.Authentication;

public class ClientGameManager : IDisposable
{
    private JoinAllocation allocation;
    private NetworkClient networkClient;
    private const string MenuSceneName = "Menu";

    public async Task<bool> InitAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"UnityServices initialization failed: {ex}");
            return false;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null.");
            return false;
        }

        networkClient = new NetworkClient(NetworkManager.Singleton);

        AuthState authState;
        try
        {
            authState = await AuthenticationWrapper.DoAuth();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Authentication failed: {ex}");
            return false;
        }

        if (authState == AuthState.Authenticated)
        {
            return true;
        }

        Debug.LogWarning($"Authentication state: {authState}");
        return false;
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    public async Task StartClientAsync(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join code is null or empty.");
            return;
        }
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join allocation: {e}");
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

        string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name");
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
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start client: {ex}");
        }
    }

    public void Disconnect()
    {
        if (networkClient != null)
        {
            networkClient.Disconnect();
        }
        else
        {
            Debug.LogWarning("NetworkClient is null on disconnect.");
        }
    }

    public void Dispose()
    {
        networkClient?.Dispose();
    }
}

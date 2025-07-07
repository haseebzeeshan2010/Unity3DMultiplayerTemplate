using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkClient : IDisposable
{
    private NetworkManager networkManager;
    private const string MenuSceneName = "Menu";
    private bool disposed = false;

    public NetworkClient(NetworkManager networkManager)
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager passed to NetworkClient is null.");
            throw new ArgumentNullException(nameof(networkManager));
        }
        this.networkManager = networkManager;
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager is null in OnClientDisconnect.");
            return;
        }
        if (clientId != 0 && clientId != networkManager.LocalClientId) { return; }
        Disconnect();
    }

    public void Disconnect()
    {
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager is null in Disconnect.");
            return;
        }
        if (SceneManager.GetActiveScene().name != MenuSceneName)
        {
            try
            {
                SceneManager.LoadScene(MenuSceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load menu scene: {ex}");
            }
        }
        if (networkManager.IsConnectedClient)
        {
            try
            {
                networkManager.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during networkManager shutdown: {ex}");
            }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
        networkManager = null;
    }
}

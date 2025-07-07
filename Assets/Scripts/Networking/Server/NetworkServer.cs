using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Collections;

public class NetworkServer : IDisposable
{
    private NetworkManager networkManager; // The network manager instance.
    public Action<string> OnClientLeft; // Invoked when a client disconnects.
    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>(); // Maps client IDs to authentication IDs.
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>(); // Maps authentication IDs to UserData objects.
    private bool disposed = false;

    // Constructor that initializes the network manager and sets up the connection approval callback.
    public NetworkServer(NetworkManager networkManager)
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager passed to NetworkServer is null.");
            throw new ArgumentNullException(nameof(networkManager));
        }
        this.networkManager = networkManager;
        networkManager.ConnectionApprovalCallback += ApprovalCheck; // Possibly change method to ConnectionApprovalCheck
        networkManager.OnServerStarted += OnNetworkReady; // Invoked when the server starts.
    }

    // to approve the connection, we need to check if the user is authenticated and if they are allowed to connect to the server.
    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        UserData userData = null;
        try
        {
            string payload = System.Text.Encoding.UTF8.GetString(request.Payload); // Decodes the byte array 'request.Payload' into a UTF-8 encoded string.
            userData = JsonUtility.FromJson<UserData>(payload); // Converts the JSON string into a UserData object.
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to deserialize connection payload: {ex}");
            response.Approved = false;
            response.CreatePlayerObject = false;
            return;
        }
        if (userData == null || string.IsNullOrEmpty(userData.userAuthId))
        {
            Debug.LogError("UserData is null or missing userAuthId in connection approval.");
            response.Approved = false;
            response.CreatePlayerObject = false;
            return;
        }
        // Prevent duplicate auth IDs
        if (authIdToUserData.ContainsKey(userData.userAuthId))
        {
            Debug.LogWarning($"Duplicate userAuthId detected: {userData.userAuthId}. Rejecting connection.");
            response.Approved = false;
            response.CreatePlayerObject = false;
            return;
        }
        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId; // Maps the client ID to the authentication ID.
        authIdToUserData[userData.userAuthId] = userData; // Maps the authentication ID to the UserData object.
        response.Approved = true; // Approves the connection.
        response.Position = SpawnPoint.GetRandomSpawnPos(); // Sets the spawn position for the player object.
        response.Rotation = Quaternion.identity; // Sets the rotation for the player object.
        response.CreatePlayerObject = true; // Creates a player object for the connection.
    }

    // Invoked when the server is ready to accept connections.
    private void OnNetworkReady()
    {
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager is null in OnNetworkReady.");
            return;
        }
        networkManager.OnClientDisconnectCallback += OnClientDisconnect; // Invoked when a client disconnects.
    }

    public UserData GetUserDataByClientId(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId)) // Checks if the authentication ID exists in the dictionary.
        {
            if (authIdToUserData.TryGetValue(authId, out UserData data)) // Checks if the UserData object exists in the dictionary.
            {
                return data; // Returns the UserData object.
            }
            return null; // Returns null if the UserData object does not exist.
        }
        return null; // Returns null if the authentication ID does not exist.
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            clientIdToAuth.Remove(clientId); // Removes the client ID from the dictionary.
            authIdToUserData.Remove(authId); // Removes the authentication ID from the dictionary.
            try
            {
                OnClientLeft?.Invoke(authId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in OnClientLeft event: {ex}");
            }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (networkManager == null) { return; } // Checks if the network manager is null.
        networkManager.ConnectionApprovalCallback -= ApprovalCheck; // Unsubscribes from the connection approval callback.
        networkManager.OnServerStarted -= OnNetworkReady; // Unsubscribes from the server started callback.
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect; // Unsubscribes from the client disconnect callback.   
        if (networkManager.IsListening)
        {
            try
            {
                networkManager.Shutdown(); // Shuts down the network manager.
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during networkManager shutdown: {ex}");
            }
        }
        networkManager = null;
    }
}


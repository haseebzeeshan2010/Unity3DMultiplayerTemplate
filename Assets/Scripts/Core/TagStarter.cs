using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles random player tagging coordination. Does not require NetworkBehaviour 
/// since it only coordinates local events and modifies existing NetworkVariables.
/// </summary>
public class TagStarter : MonoBehaviour
{
    private bool isProcessing = false;

    private void OnEnable()
    {
        NetworkTimer.CountdownBegan += OnCountdownBegan;
    }

    private void OnDisable()
    {
        NetworkTimer.CountdownBegan -= OnCountdownBegan;
    }

    private void OnCountdownBegan()
    {
        SelectRandomPlayerToTag();
    }

    public void SelectRandomPlayerToTag()
    {
        // Check if we're the server/host in the Relay session
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || isProcessing) 
            return;
            
        StartCoroutine(SelectRandomPlayerCoroutine());
    }

    private System.Collections.IEnumerator SelectRandomPlayerCoroutine()
    {
        isProcessing = true;

        try
        {
            // 3-second delay before tagging begins (gives players preparation time)
            yield return new WaitForSeconds(3f);

            // Validate server authority after delay (important for Relay host authority)
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) 
                yield break;

            var connectedClients = NetworkManager.Singleton.ConnectedClientsList;
            
            if (connectedClients.Count == 0)
            {
                Debug.LogWarning("[TagStarter] No connected clients available for tagging.");
                yield break;
            }

            // Select and tag random player (server-authoritative)
            int randomIndex = Random.Range(0, connectedClients.Count);
            var selectedClient = connectedClients[randomIndex];
            
            if (selectedClient.PlayerObject?.GetComponent<Player>() is Player playerComponent)
            {
                // Modify the NetworkVariable (server authority maintained)
                playerComponent.TagStatus.Value = Player.TagState.Tagged;
                Debug.Log($"[TagStarter] Tagged random player - Client ID: {selectedClient.ClientId}");
            }
            else
            {
                Debug.LogWarning($"[TagStarter] Invalid player object for Client ID: {selectedClient.ClientId}");
            }
        }
        finally
        {
            isProcessing = false;
        }
    }
}
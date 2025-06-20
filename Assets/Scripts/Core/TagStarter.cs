using UnityEngine;
using Unity.Netcode;

public class TagStarter : NetworkBehaviour
{
    private bool isChecking = false;

    public void StartCheckingForSinglePlayer()
    {
        if (!IsServer) return; // Ensure only server runs this
        if (!isChecking)
            StartCoroutine(CheckForSinglePlayerCoroutine());
    }

    // Example: Call this in OnNetworkSpawn to start checking when the object is spawned on the network
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCheckingForSinglePlayer();
    }

    private System.Collections.IEnumerator CheckForSinglePlayerCoroutine()
    {
        isChecking = true;
        while (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList.Count != 1)
        {
            yield return new WaitForSeconds(0.5f);
        }
        isChecking = false;
        // Single player found, you can add your logic here if needed
        if (!IsServer) yield break; // Double check server-side
        var playerObject = NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject;
        var playerComponent = playerObject != null ? playerObject.GetComponent<Player>() : null;
        if (playerComponent != null)
        {
            playerComponent.TagStatus.Value = Player.TagState.Tagged; // Set the player to be taggable
        }
    }
}

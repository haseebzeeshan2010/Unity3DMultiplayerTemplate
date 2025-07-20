using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class TagCounter : NetworkBehaviour
{
    private Player player;
    private string username = string.Empty;
    
    [Tooltip("Total time the player has been tagged, in seconds")]
    public NetworkVariable<float> totalTaggedTime = new NetworkVariable<float>(0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    // Timestamp when tagging started, negative if not currently tagged
    private float tagStartTime = -1f;

    // Read-only accessors for other systems
    public float TotalTaggedTime => totalTaggedTime.Value;
    public string Username => username;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        player = GetComponent<Player>();
        if (player != null)
        {
            // Cache the username from the player's NetworkVariable
            username = player.PlayerName.Value.ToString();
            
            // Subscribe to tag status changes for timing calculations
            player.TagStatus.OnValueChanged += OnTagStatusChanged;
            
            // Subscribe to username changes in case it updates during gameplay
            player.PlayerName.OnValueChanged += OnPlayerNameChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (player != null)
        {
            player.TagStatus.OnValueChanged -= OnTagStatusChanged;
            player.PlayerName.OnValueChanged -= OnPlayerNameChanged;
        }

        base.OnNetworkDespawn();
    }

    private void OnPlayerNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        username = current.ToString();
    }

    private void OnTagStatusChanged(Player.TagState previous, Player.TagState current)
    {
        // Only the server should calculate and update timing
        if (!IsServer) return;

        if (current == Player.TagState.Tagged)
        {
            // Begin timing when tagged
            tagStartTime = Time.time;
        }
        else if (previous == Player.TagState.Tagged && tagStartTime >= 0f)
        {
            // Accumulate duration and reset
            totalTaggedTime.Value += Time.time - tagStartTime;
            tagStartTime = -1f;
        }
    }

    // Utility methods
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(username) ? "Unknown Player" : username;
    }

    public bool HasValidUsername()
    {
        return !string.IsNullOrEmpty(username);
    }
}
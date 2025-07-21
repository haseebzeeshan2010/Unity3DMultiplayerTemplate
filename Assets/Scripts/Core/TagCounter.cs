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
    
    [Tooltip("How often to sync live tagged time over network (0 = only on tag end)")]
    [SerializeField] private float liveSyncInterval = 1f; // Sync every second while tagged
    private float lastSyncTime = 0f;

    // Read-only accessors for other systems
    public float TotalTaggedTime => GetCurrentTotalTime();
    public string Username => username;

    // Returns total time including current tag session if active
    private float GetCurrentTotalTime()
    {
        float baseTime = totalTaggedTime.Value;
        
        // If currently tagged, add elapsed time since tag started
        if (tagStartTime >= 0f)
        {
            baseTime += Time.time - tagStartTime;
        }
        
        return baseTime;
    }

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

    private void Update()
    {
        // Server-side: Periodically sync live tagged time to network
        if (IsServer && tagStartTime >= 0f && liveSyncInterval > 0f)
        {
            if (Time.time - lastSyncTime >= liveSyncInterval)
            {
                // Update NetworkVariable with current accumulated time
                totalTaggedTime.Value += Time.time - tagStartTime;
                tagStartTime = Time.time; // Reset start time to current
                lastSyncTime = Time.time;
            }
        }
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
            lastSyncTime = Time.time;
        }
        else if (previous == Player.TagState.Tagged && tagStartTime >= 0f)
        {
            // Final accumulation when tag ends
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
    
    // Get formatted time string for UI display
    public string GetFormattedTime()
    {
        float time = GetCurrentTotalTime();
        return $"{time:F1}s"; // Shows one decimal place
    }
}
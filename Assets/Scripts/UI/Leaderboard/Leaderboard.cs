using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Leaderboard : NetworkBehaviour
{
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderboardEntityDisplay leaderboardEntityPrefab;
    [SerializeField] private int entitiesToDisplay = 8;

    private NetworkList<LeaderboardEntityState> leaderboardEntities;
    private List<LeaderboardEntityDisplay> entityDisplays = new List<LeaderboardEntityDisplay>();

    private void Awake()
    {
        leaderboardEntities = new NetworkList<LeaderboardEntityState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // Subscribe to NetworkList changes for UI updates
            leaderboardEntities.OnListChanged += HandleLeaderboardEntitiesChanged;
            
            // Handle existing entities (late-joining clients)
            foreach (LeaderboardEntityState entity in leaderboardEntities)
            {
                HandleLeaderboardEntitiesChanged(new NetworkListEvent<LeaderboardEntityState>
                {
                    Type = NetworkListEvent<LeaderboardEntityState>.EventType.Add,
                    Value = entity
                });
            }
        }

        if (IsServer)
        {
            // Find existing players and register them
            Player[] existingPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (Player player in existingPlayers)
            {
                HandlePlayerSpawned(player);
            }

            // Subscribe to player lifecycle events
            Player.OnPlayerSpawned += HandlePlayerSpawned;
            Player.OnPlayerDespawned += HandlePlayerDespawned;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged -= HandleLeaderboardEntitiesChanged;
        }

        if (IsServer)
        {
            Player.OnPlayerSpawned -= HandlePlayerSpawned;
            Player.OnPlayerDespawned -= HandlePlayerDespawned;
        }
    }

    private void HandlePlayerSpawned(Player player)
    {
        TagCounter tagCounter = player.GetComponent<TagCounter>();
        if (tagCounter == null)
        {
            Debug.LogWarning($"Player {player.OwnerClientId} spawned without TagCounter component!");
            return;
        }

        // Add player to leaderboard with initial tag time
        leaderboardEntities.Add(new LeaderboardEntityState
        {
            ClientId = player.OwnerClientId,
            PlayerName = player.PlayerName.Value,
            TagTimed = Mathf.FloorToInt(tagCounter.TotalTaggedTime) // Repurpose TagTimed field for tag time
        });

        // Subscribe to tag time changes for this player
        tagCounter.totalTaggedTime.OnValueChanged += (oldTime, newTime) =>
            HandleTagTimeChanged(player.OwnerClientId, newTime);
    }

    private void HandlePlayerDespawned(Player player)
    {
        TagCounter tagCounter = player.GetComponent<TagCounter>();
        if (tagCounter == null) return;

        // Remove player from leaderboard
        for (int i = leaderboardEntities.Count - 1; i >= 0; i--)
        {
            if (leaderboardEntities[i].ClientId == player.OwnerClientId)
            {
                leaderboardEntities.RemoveAt(i);
                break;
            }
        }

        // Unsubscribe from tag time changes
        tagCounter.totalTaggedTime.OnValueChanged -= (oldTime, newTime) =>
            HandleTagTimeChanged(player.OwnerClientId, newTime);
    }

    private void HandleTagTimeChanged(ulong clientId, float newTagTime)
    {
        // Update the leaderboard entity with new tag time
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId != clientId) continue;

            leaderboardEntities[i] = new LeaderboardEntityState
            {
                ClientId = leaderboardEntities[i].ClientId,
                PlayerName = leaderboardEntities[i].PlayerName,
                TagTimed = Mathf.FloorToInt(newTagTime) // Store tag time as integer seconds
            };
            return;
        }
    }

    private void HandleLeaderboardEntitiesChanged(NetworkListEvent<LeaderboardEntityState> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<LeaderboardEntityState>.EventType.Add:
                // Create new display entity if it doesn't exist
                if (!entityDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
                {
                    LeaderboardEntityDisplay leaderboardEntity =
                        Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder);
                    leaderboardEntity.Initialise(
                        changeEvent.Value.ClientId,
                        changeEvent.Value.PlayerName,
                        changeEvent.Value.TagTimed); // TagTimed field contains tag time
                    entityDisplays.Add(leaderboardEntity);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Remove:
                // Remove display entity
                LeaderboardEntityDisplay displayToRemove =
                    entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToRemove != null)
                {
                    displayToRemove.transform.SetParent(null);
                    Destroy(displayToRemove.gameObject);
                    entityDisplays.Remove(displayToRemove);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Value:
                // Update existing display entity
                LeaderboardEntityDisplay displayToUpdate =
                    entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToUpdate != null)
                {
                    displayToUpdate.UpdateTagTime(changeEvent.Value.TagTimed); // Update with new tag time
                }
                break;
        }

        // Sort by tag time (highest first - most tagged time = worst performance)
        entityDisplays.Sort((x, y) => y.TagTimed.CompareTo(x.TagTimed));

        // Update display order and visibility
        for (int i = 0; i < entityDisplays.Count; i++)
        {
            entityDisplays[i].transform.SetSiblingIndex(i);
            entityDisplays[i].UpdateText();
            bool shouldShow = i <= entitiesToDisplay - 1;
            entityDisplays[i].gameObject.SetActive(shouldShow);
        }

        // Always show local player if they're outside top N
        LeaderboardEntityDisplay myDisplay = 
            entityDisplays.FirstOrDefault(x => x.ClientId == NetworkManager.Singleton.LocalClientId);
        if (myDisplay != null && myDisplay.transform.GetSiblingIndex() >= entitiesToDisplay)
        {
            leaderboardEntityHolder.GetChild(entitiesToDisplay - 1).gameObject.SetActive(false);
            myDisplay.gameObject.SetActive(true);
        }
    }
}
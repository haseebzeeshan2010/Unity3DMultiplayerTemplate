using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using DG.Tweening; // Add DOTween reference

public class Leaderboard : NetworkBehaviour
{
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderboardEntityDisplay leaderboardEntityPrefab;
    [SerializeField] private int entitiesToDisplay = 8;

    [Header("Animation Settings")]
    [SerializeField] private float itemHeight = 60f; // Height of each leaderboard item
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float staggerDelay = 0.02f; // Delay between each item animation
    [SerializeField] private Ease animationEase = Ease.OutQuart;

    [Header("Player Connection Check")]
    [SerializeField] private float connectionCheckInterval = 2f; // Check every 2 seconds

    private NetworkList<LeaderboardEntityState> leaderboardEntities;
    private List<LeaderboardEntityDisplay> entityDisplays = new List<LeaderboardEntityDisplay>();
    private Sequence currentAnimationSequence;
    private Coroutine connectionCheckCoroutine;

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

            // Start connection check coroutine
            connectionCheckCoroutine = StartCoroutine(CheckPlayerConnections());
        }
    }

    public override void OnNetworkDespawn()
    {
        // Kill any running animations
        currentAnimationSequence?.Kill();

        // Stop connection check coroutine
        if (connectionCheckCoroutine != null)
        {
            StopCoroutine(connectionCheckCoroutine);
            connectionCheckCoroutine = null;
        }

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

    private IEnumerator CheckPlayerConnections()
    {
        while (IsServer && NetworkManager.Singleton != null)
        {
            yield return new WaitForSeconds(connectionCheckInterval);

            // Check each leaderboard entity to see if the player is still connected
            for (int i = leaderboardEntities.Count - 1; i >= 0; i--)
            {
                ulong clientId = leaderboardEntities[i].ClientId;

                // Check if player is still connected using NetworkManager
                if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                {
                    Debug.Log($"Player {clientId} disconnected, removing from leaderboard");

                    // Remove from leaderboard (this will trigger network sync and destroy the object for all clients)
                    leaderboardEntities.RemoveAt(i);
                }
            }
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
            TagTimed = Mathf.FloorToInt(tagCounter.TotalTaggedTime)
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
                TagTimed = Mathf.FloorToInt(newTagTime)
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
                if (!entityDisplays.Any(x => x != null && x.ClientId == changeEvent.Value.ClientId))
                {
                    LeaderboardEntityDisplay leaderboardEntity =
                        Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder);

                    // Initialize at bottom position (off-screen) for smooth entry animation
                    Vector3 startPosition = new Vector3(0, -itemHeight * entityDisplays.Count, 0);
                    leaderboardEntity.transform.localPosition = startPosition;

                    leaderboardEntity.Initialise(
                        changeEvent.Value.ClientId,
                        changeEvent.Value.PlayerName,
                        changeEvent.Value.TagTimed);
                    entityDisplays.Add(leaderboardEntity);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Remove:
                // Remove display entity with fade-out animation
                LeaderboardEntityDisplay displayToRemove =
                    entityDisplays.FirstOrDefault(x => x != null && x.ClientId == changeEvent.Value.ClientId);
                if (displayToRemove != null)
                {
                    // Remove from list immediately to prevent access during animation
                    entityDisplays.Remove(displayToRemove);

                    // Animate removal
                    displayToRemove.transform.DOScale(0f, animationDuration * 0.5f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            if (displayToRemove != null)
                            {
                                displayToRemove.transform.SetParent(null);
                                Destroy(displayToRemove.gameObject);
                            }
                        });
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Value:
                // Update existing display entity
                LeaderboardEntityDisplay displayToUpdate =
                    entityDisplays.FirstOrDefault(x => x != null && x.ClientId == changeEvent.Value.ClientId);
                if (displayToUpdate != null)
                {
                    displayToUpdate.UpdateTagTime(changeEvent.Value.TagTimed);
                }
                break;
        }

        // Animate to new positions after any change
        AnimateToNewPositions();
    }

    private void AnimateToNewPositions()
    {
        // Clean up null references first
        entityDisplays.RemoveAll(x => x == null);

        // Kill any existing animation sequence
        currentAnimationSequence?.Kill();

        // Sort by tag time (highest first - most tagged time = worst performance)
        // Filter out null references before sorting
        var validDisplays = entityDisplays.Where(x => x != null).ToList();
        validDisplays.Sort((x, y) => y.TagTimed.CompareTo(x.TagTimed));

        // Update the main list with cleaned and sorted data
        entityDisplays = validDisplays;

        // Create new animation sequence
        currentAnimationSequence = DOTween.Sequence();

        // Animate each entity to its new position
        for (int i = 0; i < entityDisplays.Count; i++)
        {
            LeaderboardEntityDisplay display = entityDisplays[i];

            // Double-check for null before accessing
            if (display == null) continue;

            Vector3 targetPosition = new Vector3(0, -i * itemHeight, 0);

            // Determine visibility based on rank
            bool shouldShow = i < entitiesToDisplay;

            // Create position tween
            Tween positionTween = display.transform.DOLocalMove(targetPosition, animationDuration)
                .SetEase(animationEase);

            // Create visibility tween if needed
            if (shouldShow && !display.gameObject.activeSelf)
            {
                display.gameObject.SetActive(true);
                display.transform.localScale = Vector3.zero;
                Tween scaleTween = display.transform.DOScale(Vector3.one, animationDuration * 0.5f)
                    .SetEase(Ease.OutBack);
                currentAnimationSequence.Join(scaleTween);
            }
            else if (!shouldShow && display.gameObject.activeSelf)
            {
                Tween fadeOutTween = display.transform.DOScale(Vector3.zero, animationDuration * 0.5f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        if (display != null && display.gameObject != null)
                            display.gameObject.SetActive(false);
                    });
                currentAnimationSequence.Join(fadeOutTween);
            }

            // Add position tween to sequence with stagger
            if (i == 0)
                currentAnimationSequence.Append(positionTween);
            else
                currentAnimationSequence.Join(positionTween.SetDelay(i * staggerDelay));

            // Update text after position animation - capture the display reference
            LeaderboardEntityDisplay capturedDisplay = display;
            currentAnimationSequence.AppendCallback(() =>
            {
                if (capturedDisplay != null)
                    capturedDisplay.UpdateText();
            });
        }

        // Handle local player visibility (always show if outside top N)
        LeaderboardEntityDisplay myDisplay =
            entityDisplays.FirstOrDefault(x => x != null && x.ClientId == NetworkManager.Singleton.LocalClientId);
        if (myDisplay != null)
        {
            int myRank = entityDisplays.IndexOf(myDisplay);
            if (myRank >= entitiesToDisplay)
            {
                // Hide the last visible item and show local player
                if (entityDisplays.Count > entitiesToDisplay)
                {
                    LeaderboardEntityDisplay lastVisible = entityDisplays[entitiesToDisplay - 1];
                    if (lastVisible != null)
                    {
                        Tween hideLastTween = lastVisible.transform.DOScale(Vector3.zero, animationDuration * 0.3f)
                            .SetEase(Ease.InBack)
                            .OnComplete(() =>
                            {
                                if (lastVisible != null && lastVisible.gameObject != null)
                                    lastVisible.gameObject.SetActive(false);
                            });
                        currentAnimationSequence.Join(hideLastTween);
                    }
                }

                // Show local player
                if (!myDisplay.gameObject.activeSelf)
                {
                    myDisplay.gameObject.SetActive(true);
                    myDisplay.transform.localScale = Vector3.zero;
                    Tween showMyTween = myDisplay.transform.DOScale(Vector3.one, animationDuration * 0.5f)
                        .SetEase(Ease.OutBack);
                    currentAnimationSequence.Join(showMyTween);
                }
            }
        }
    }
}
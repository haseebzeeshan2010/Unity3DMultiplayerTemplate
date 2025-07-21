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

    private NetworkList<LeaderboardEntityState> leaderboardEntities;
    private List<LeaderboardEntityDisplay> entityDisplays = new List<LeaderboardEntityDisplay>();
    private Sequence currentAnimationSequence;

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
        // Kill any running animations
        currentAnimationSequence?.Kill();

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
                if (!entityDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
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
                    entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToRemove != null)
                {
                    // Animate removal
                    displayToRemove.transform.DOScale(0f, animationDuration * 0.5f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            displayToRemove.transform.SetParent(null);
                            Destroy(displayToRemove.gameObject);
                        });
                    entityDisplays.Remove(displayToRemove);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Value:
                // Update existing display entity
                LeaderboardEntityDisplay displayToUpdate =
                    entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
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
        // Kill any existing animation sequence
        // currentAnimationSequence?.Kill();

        // Sort by tag time (highest first - most tagged time = worst performance)
        entityDisplays.Sort((x, y) => y.TagTimed.CompareTo(x.TagTimed));

        // Create new animation sequence
        currentAnimationSequence = DOTween.Sequence();

        // Animate each entity to its new position
        for (int i = 0; i < entityDisplays.Count; i++)
        {
            LeaderboardEntityDisplay display = entityDisplays[i];
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
                    .OnComplete(() => display.gameObject.SetActive(false));
                currentAnimationSequence.Join(fadeOutTween);
            }

            // Add position tween to sequence with stagger
            if (i == 0)
                currentAnimationSequence.Append(positionTween);
            else
                currentAnimationSequence.Join(positionTween.SetDelay(i * staggerDelay));

            // Update text after position animation
            currentAnimationSequence.AppendCallback(() => display.UpdateText());
        }

        // Handle local player visibility (always show if outside top N)
        LeaderboardEntityDisplay myDisplay =
            entityDisplays.FirstOrDefault(x => x.ClientId == NetworkManager.Singleton.LocalClientId);
        if (myDisplay != null)
        {
            int myRank = entityDisplays.IndexOf(myDisplay);
            if (myRank >= entitiesToDisplay)
            {
                // Hide the last visible item and show local player
                if (entityDisplays.Count > entitiesToDisplay)
                {
                    LeaderboardEntityDisplay lastVisible = entityDisplays[entitiesToDisplay - 1];
                    Tween hideLastTween = lastVisible.transform.DOScale(Vector3.zero, animationDuration * 0.3f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => lastVisible.gameObject.SetActive(false));
                    currentAnimationSequence.Join(hideLastTween);
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
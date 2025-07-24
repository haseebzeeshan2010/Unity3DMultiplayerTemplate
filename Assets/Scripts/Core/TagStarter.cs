using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles random player tagging coordination. Does not require NetworkBehaviour 
/// since it only coordinates local events and modifies existing NetworkVariables.
/// 
/// CORE FUNCTIONALITY:
/// - Listens for countdown events to initiate tagging
/// - Selects random players for initial tagging (server authority)
/// - Validates tagged players periodically to ensure game continuity
/// - Manages coroutine lifecycle for delayed/periodic operations
/// </summary>
public class TagStarter : MonoBehaviour
{
    [SerializeField] private float tagValidationInterval = 5f;
    [SerializeField] private float tagSelectionDelay = 3f;
    
    // CORE STATE: Processing flag to prevent concurrent tag operations
    private bool isProcessing = false;
    private Coroutine tagValidationCoroutine;

    // CORE UTILITY: Server authority check using NGO 2.4.1 pattern
    private bool IsServerReady => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    #region CORE LIFECYCLE: Event subscription and cleanup
    
    private void OnEnable()
    {
        // CORE HOOK: Subscribe to countdown events for tag initiation
        NetworkTimer.CountdownBegan += OnCountdownBegan;
    }

    private void OnDisable()
    {
        // CORE CLEANUP: Unsubscribe and stop validation to prevent memory leaks
        NetworkTimer.CountdownBegan -= OnCountdownBegan;
        StopTagValidation();
    }
    
    #endregion

    #region CORE FUNCTIONALITY: Initial tag selection on countdown

    /// <summary>
    /// CORE EVENT HANDLER: Responds to countdown beginning by starting tag process
    /// </summary>
    private void OnCountdownBegan()
    {
        SelectRandomPlayerToTag();
        StartTagValidation();
    }

    /// <summary>
    /// CORE ENTRY POINT: Initiates delayed random player tagging (server authority)
    /// </summary>
    public void SelectRandomPlayerToTag()
    {
        // CORE GUARD: Ensure server authority and prevent concurrent operations
        if (!IsServerReady || isProcessing) return;
        
        StartCoroutine(DelayedTagSelection());
    }

    /// <summary>
    /// CORE DELAY MECHANISM: Provides grace period before tagging for game flow
    /// </summary>
    private IEnumerator DelayedTagSelection()
    {
        // CORE STATE: Lock processing to prevent race conditions
        isProcessing = true;
        
        yield return new WaitForSeconds(tagSelectionDelay);
        
        // CORE VALIDATION: Re-check server status after delay
        if (IsServerReady)
        {
            TagRandomPlayer();
        }
        
        // CORE STATE: Release processing lock
        isProcessing = false;
    }

    #endregion

    #region CORE ALGORITHM: Random player selection and tagging

    /// <summary>
    /// CORE TAGGING LOGIC: Selects and tags a random connected player
    /// Uses NGO 2.4.1 NetworkVariable pattern for state synchronization
    /// </summary>
    private void TagRandomPlayer()
    {
        // CORE DATA: Get all valid players for tagging
        var availablePlayers = GetConnectedPlayers();
        
        // CORE GUARD: Ensure players exist before tagging
        if (availablePlayers.Count == 0)
        {
            Debug.LogWarning("[TagStarter] No connected clients available for tagging.");
            return;
        }

        // CORE SELECTION: Random player choice with Unity's Random system
        var randomPlayer = availablePlayers[Random.Range(0, availablePlayers.Count)];
        
        // CORE STATE CHANGE: Modify NetworkVariable (automatically syncs to all clients)
        randomPlayer.TagStatus.Value = Player.TagState.Tagged;
        
        Debug.Log($"[TagStarter] Tagged random player - Client ID: {randomPlayer.OwnerClientId}");
    }

    /// <summary>
    /// CORE DATA SOURCE: Retrieves all connected players using NGO 2.4.1 client management
    /// </summary>
    private List<Player> GetConnectedPlayers()
    {
        var players = new List<Player>();
        
        // CORE ITERATION: Use NGO 2.4.1 ConnectedClientsList for reliable client enumeration
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // CORE COMPONENT ACCESS: Safe component retrieval with null-conditional operator
            if (client.PlayerObject?.GetComponent<Player>() is Player player)
            {
                players.Add(player);
            }
        }
        
        return players;
    }

    #endregion

    #region CORE VALIDATION: Periodic tag state monitoring

    /// <summary>
    /// CORE VALIDATION LOOP: Ensures at least one player remains tagged
    /// Prevents game state where no tagged players exist
    /// </summary>
    private IEnumerator ValidateTaggedPlayersPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(tagValidationInterval);

            // CORE AUTHORITY: Only server performs validation
            if (!IsServerReady) continue;

            // CORE CHECK: Validate tagged player existence
            if (!HasAnyTaggedPlayers())
            {
                Debug.Log("[TagStarter] No tagged players detected. Selecting new random player...");
                SelectRandomPlayerToTag();
            }
        }
    }

    /// <summary>
    /// CORE STATE CHECKER: Determines if any players are currently tagged
    /// </summary>
    private bool HasAnyTaggedPlayers()
    {
        var players = GetConnectedPlayers();
        
        // CORE VALIDATION: Check each player's NetworkVariable tag status
        foreach (var player in players)
        {
            if (player.TagStatus.Value == Player.TagState.Tagged)
                return true;
        }
        
        // CORE EDGE CASE: Return true if no players to avoid unnecessary tagging
        return players.Count == 0;
    }

    #endregion

    #region CORE LIFECYCLE: Validation coroutine management

    /// <summary>
    /// CORE CLEANUP: Safely stops periodic validation coroutine
    /// </summary>
    public void StopTagValidation()
    {
        if (tagValidationCoroutine != null)
        {
            StopCoroutine(tagValidationCoroutine);
            tagValidationCoroutine = null;
            Debug.Log("[TagStarter] Tag validation stopped.");
        }
    }

    /// <summary>
    /// CORE STARTUP: Initiates periodic tag validation if not already running
    /// </summary>
    public void StartTagValidation()
    {
        if (tagValidationCoroutine == null)
        {
            tagValidationCoroutine = StartCoroutine(ValidateTaggedPlayersPeriodically());
            Debug.Log("[TagStarter] Tag validation started.");
        }
    }

    #endregion
}
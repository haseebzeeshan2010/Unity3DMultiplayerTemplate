using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening; // Add DOTween reference

public class LeaderboardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text UsernameText;
    [SerializeField] private TMP_Text ScoreText;
    [SerializeField] private Color myColour;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Ease fadeOutEase = Ease.InQuart;

    private FixedString32Bytes playerName;
    private Coroutine connectionCheckCoroutine;
    private CanvasGroup canvasGroup;
    private bool isBeingDestroyed = false;

    public static event Action<ulong> OnPlayerDisconnected;

    public ulong ClientId { get; private set; }
    public int TagTimed { get; private set; }

    private void Awake()
    {
        // Subscribe to disconnection events
        OnPlayerDisconnected += HandlePlayerDisconnected;
        
        // Ensure we have a CanvasGroup for fade animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Start with full alpha
        canvasGroup.alpha = 1f;
    }

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int tagTimes)
    {
        ClientId = clientId;
        this.playerName = playerName;
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UsernameText.color = myColour;
        }
        UpdateTagTime(tagTimes);
        
        // Start checking player connection status (only on server/host)
        if (NetworkManager.Singleton.IsServer)
        {
            connectionCheckCoroutine = StartCoroutine(CheckPlayerConnection());
        }
    }

    private IEnumerator CheckPlayerConnection()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            
            // Check if NetworkManager is still valid and if client is still connected
            if (NetworkManager.Singleton != null && 
                !NetworkManager.Singleton.ConnectedClients.ContainsKey(ClientId))
            {
                // Player disconnected - trigger event for all local instances
                OnPlayerDisconnected?.Invoke(ClientId);
                yield break; // Exit the coroutine
            }
        }
    }

    private void HandlePlayerDisconnected(ulong disconnectedClientId)
    {
        if (ClientId == disconnectedClientId && !isBeingDestroyed)
        {
            // This leaderboard entry belongs to the disconnected player
            StartFadeOutAndDestroy();
        }
    }

    private void StartFadeOutAndDestroy()
    {
        isBeingDestroyed = true;
        
        // Stop the connection check coroutine if it's running
        if (connectionCheckCoroutine != null)
        {
            StopCoroutine(connectionCheckCoroutine);
            connectionCheckCoroutine = null;
        }

        // Animate fade out with CanvasGroup alpha
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(fadeOutEase)
                .OnComplete(() =>
                {
                    // Add a small delay before destroying the GameObject
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        if (gameObject != null)
                        {
                            Destroy(gameObject);
                        }
                    });
                });
            
            
        }
    }

    public void UpdateTagTime(int tagTimes)
    {
        // Don't update if we're being destroyed
        if (isBeingDestroyed) return;
        
        TagTimed = tagTimes;
        
        // Animate score change with a subtle pulse effect
        if (ScoreText != null)
        {
            ScoreText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 1, 0.5f);
        }
        
        UpdateText();
    }

    public void UpdateText()
    {
        // Don't update text if we're being destroyed
        if (isBeingDestroyed) return;
        
        UsernameText.text = $"{playerName}";
        ScoreText.text = $"{TagTimed}s"; // Add 's' for seconds
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        OnPlayerDisconnected -= HandlePlayerDisconnected;
        
        // Stop the connection check coroutine if it's running
        if (connectionCheckCoroutine != null)
        {
            StopCoroutine(connectionCheckCoroutine);
        }
        
        // Kill any running tweens on this object and its children
        transform.DOKill();
        if (ScoreText != null)
            ScoreText.transform.DOKill();
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }
}
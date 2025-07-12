using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkTimer : NetworkBehaviour
{
    [SerializeField] private float timerDuration = 120f; // Main timer duration
    [SerializeField] private float countdownDuration = 3f; // Countdown period before timer starts

    [SerializeField] private TextMeshProUGUI timerText;      // Main timer UI text
    [SerializeField] private TextMeshProUGUI countdownText;  // Countdown UI text
    [SerializeField] private GameObject goText;
    [SerializeField] private GameObject ClientTextObject;    // Additional client-only UI
    [SerializeField] private GameObject TimerStartButton;    // UI button for host to start timer
    [SerializeField] private GameObject TimerVisibility;     // Container for timer UI

    private readonly NetworkVariable<double> _endTime = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _isTimerRunning = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool _hasEndedLocally = false;
    private bool _hasTriggeredCountdown = false; // Guard flag to fire the event once

    // This event is raised when the countdown starts.
    public static event Action CountdownBegan;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient && !IsHost)
        {
            TimerVisibility.SetActive(false);
            ClientTextObject.SetActive(true);
            TimerStartButton.SetActive(false);
        }

        _isTimerRunning.OnValueChanged += (prevValue, newValue) =>
        {
            if (newValue)
            {
                ClientTextObject.SetActive(false);
                TimerVisibility.SetActive(true);
            }
        };

        // At start, hide both UI elements
        timerText.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsClient) return;
        if (!_isTimerRunning.Value) return;

        double remaining = _endTime.Value - NetworkManager.ServerTime.Time;

        // Countdown phase: remaining time is greater than timerDuration.
        if (remaining > timerDuration)
        {
            // Fire event only once when countdown starts.
            if (!_hasTriggeredCountdown)
            {
                _hasTriggeredCountdown = true;
                CountdownBegan?.Invoke();
            }

            int countdownSeconds = Mathf.CeilToInt((float)(remaining - timerDuration));
            countdownText.text = $"{countdownSeconds}";
            countdownText.gameObject.SetActive(true);
            timerText.gameObject.SetActive(false);
            _hasEndedLocally = false;
            return;
        }
        else // Main timer phase
        {
            goText.SetActive(true);
            ClientTextObject.SetActive(false);
            int minutes = Mathf.FloorToInt((float)remaining / 60);
            int seconds = Mathf.FloorToInt((float)remaining % 60);
            timerText.text = $"{minutes:0}:{seconds:00}";
            timerText.gameObject.SetActive(true);
            countdownText.gameObject.SetActive(false);
            _hasEndedLocally = false;
        }

        // End logic
        if (remaining <= 0 && _isTimerRunning.Value && !_hasEndedLocally)
        {
            timerText.text = "0:00";
            _hasEndedLocally = true;
            OnTimerEnded();
        }
    }

    // Call this from UI (host only)
    public void StartTimerFromUI()
    {
        if (IsHost)
        {
            // Total duration includes the countdown period.
            StartTimerServerRpc(timerDuration + countdownDuration);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartTimerServerRpc(float totalDuration)
    {
        _endTime.Value = NetworkManager.ServerTime.Time + totalDuration;
        _isTimerRunning.Value = true;
    }

    private void OnTimerEnded()
    {
        if (IsHost)
        {
            _isTimerRunning.Value = false;
        }
        // Add any client-side end-of-timer logic here.
    }
}
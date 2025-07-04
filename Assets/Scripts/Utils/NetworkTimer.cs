using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkTimer : NetworkBehaviour
{
    [SerializeField] private float timerDuration = 120f; // Default duration
    [SerializeField] private TextMeshProUGUI timerText; // Assign in inspector

    private readonly NetworkVariable<double> _endTime = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _isTimerRunning = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool _hasEndedLocally = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsClient)
        {
            // Initially hide the timer text
            timerText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsClient) return;

        // Show/hide timer text based on timer state
        timerText.gameObject.SetActive(_isTimerRunning.Value);

        double remaining = _endTime.Value - NetworkManager.ServerTime.Time;

        if (remaining > 0 && _isTimerRunning.Value)
        {
            int minutes = Mathf.FloorToInt((float)remaining / 60);
            int seconds = Mathf.FloorToInt((float)remaining % 60);
            timerText.text = $"{minutes:0}:{seconds:00}";
            _hasEndedLocally = false; // Reset if timer is still running
        }
        else if (_isTimerRunning.Value && !_hasEndedLocally)
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
            StartTimerServerRpc(timerDuration);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartTimerServerRpc(float duration)
    {
        _endTime.Value = NetworkManager.ServerTime.Time + duration;
        _isTimerRunning.Value = true;
    }

    private void OnTimerEnded()
    {
        if (IsHost)
        {
            _isTimerRunning.Value = false;
        }
        // Add any client-side logic for when the timer ends
        
        
    }
}
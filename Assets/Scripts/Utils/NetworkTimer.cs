using System.Globalization;
using System.Timers;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkTimer : NetworkBehaviour
{
    [SerializeField] private float timerDuration = 120f; // Default duration
    [SerializeField] private TextMeshProUGUI timerText; // Assign in inspector
    [SerializeField] private GameObject ClientTextObject; // Assign in inspector
    [SerializeField] private GameObject TimerStartButton;
    
    [SerializeField] private GameObject TimerVisibility;

    private readonly NetworkVariable<double> _endTime = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _isTimerRunning = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool _hasEndedLocally = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsClient && !IsHost)
        {
            TimerVisibility.SetActive(false);
            ClientTextObject.SetActive(true);
            TimerStartButton.SetActive(false);
        }
        
        _isTimerRunning.OnValueChanged += (prevValue, newValue) => {
            if(newValue)
            {
                ClientTextObject.SetActive(false);
                TimerVisibility.SetActive(true);
            }
        };
    }

    void Update()
    {
        if (!IsClient) return;
        

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
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

    private FixedString32Bytes playerName;

    public ulong ClientId { get; private set; }
    public int TagTimed { get; private set; }

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int tagTimes)
    {
        ClientId = clientId;
        this.playerName = playerName;
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UsernameText.color = myColour;
        }
        UpdateTagTime(tagTimes);
    }

    public void UpdateTagTime(int tagTimes)
    {
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
        UsernameText.text = $"{playerName}";
        ScoreText.text = $"{TagTimed}s"; // Add 's' for seconds
    }

    private void OnDestroy()
    {
        // Kill any running tweens on this object
        transform.DOKill();
        if (ScoreText != null)
            ScoreText.transform.DOKill();
    }
}
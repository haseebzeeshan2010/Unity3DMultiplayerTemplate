using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

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
            UsernameText.color = myColour; //possibly capital C
        }
        UpdateTagTime(tagTimes);
    }

    public void UpdateTagTime(int tagTimes)
    {
        TagTimed = tagTimes;

        UpdateText();
    }

    public void UpdateText()
    {
        // UsernameText.text = $"{transform.GetSiblingIndex()+1}. {playerName} ({TagTimed})";
        UsernameText.text = $"{playerName}";

        ScoreText.text = $"{TagTimed}";
    }
}
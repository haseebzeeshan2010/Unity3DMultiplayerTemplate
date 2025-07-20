using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntryUI : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text timeText;

    public void Set(string playerName, float taggedTime)
    {
        nameText.text = playerName;
        timeText.text = $"{taggedTime:F2}s";
    }
}
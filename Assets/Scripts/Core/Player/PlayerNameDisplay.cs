using UnityEngine;
using TMPro;
using Unity.Collections;
public class PlayerNameDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText; // Reference to the TextMeshProUGUI component.
    [SerializeField] private TankPlayer player; // Reference to the TankPlayer component. 
    private void Start()
    {
        HandlePlayerNameChanged(string.Empty, player.PlayerName.Value); // Initialize the display name with the current player name.
    
        player.PlayerName.OnValueChanged += HandlePlayerNameChanged; // Subscribe to the PlayerName variable's value change event.
    }

    private void OnDestroy()
    {
        player.PlayerName.OnValueChanged -= HandlePlayerNameChanged; // Unsubscribe to the PlayerName variable's value change event.
    }

    private void HandlePlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        playerNameText.text = newName.ToString(); // Update the text to display the new player name.

    }
}

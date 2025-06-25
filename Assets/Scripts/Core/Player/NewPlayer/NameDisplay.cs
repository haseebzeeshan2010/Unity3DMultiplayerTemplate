using UnityEngine;
using TMPro;
using Unity.Collections;
using Unity.Cinemachine;
public class NameDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Transform trans;

    [SerializeField] private TextMeshProUGUI nameText;

    [SerializeField] private Player player; // Reference to the Player component.

    [SerializeField] private CinemachineCamera virtualCamera;

    void Start()
    {
        HandlePlayerNameChanged(string.Empty, player.PlayerName.Value); // Initialize the display name with the current player name.
    
        player.PlayerName.OnValueChanged += HandlePlayerNameChanged; // Subscribe to the PlayerName variable's value change event.
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(virtualCamera.transform); // Make the name display always face the camera.

    }
    private void OnDestroy()
    {
        player.PlayerName.OnValueChanged -= HandlePlayerNameChanged; // Unsubscribe to the PlayerName variable's value change event.
    }

    private void HandlePlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        nameText.text = newName.ToString(); // Update the text to display the new player name.

    }
}

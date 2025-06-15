using UnityEngine;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public async void StartHost()
    {
        // Start the host game manager
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    public async void StartClient()
    {
        // Start the client game manager
        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCodeField.text);
    }
}

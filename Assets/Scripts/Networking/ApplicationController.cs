using System.Threading.Tasks;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);
        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if (isDedicatedServer)
        {
            Debug.LogWarning("Dedicated server mode is not implemented.");
            // Optionally, implement server logic here.
        }
        else
        {
            if (hostPrefab == null)
            {
                Debug.LogError("Host prefab is not assigned in the inspector.");
                return;
            }
            if (clientPrefab == null)
            {
                Debug.LogError("Client prefab is not assigned in the inspector.");
                return;
            }

            HostSingleton hostSingleton = Instantiate(hostPrefab);
            if (hostSingleton == null)
            {
                Debug.LogError("Failed to instantiate HostSingleton prefab.");
                return;
            }
            hostSingleton.CreateHost();

            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            if (clientSingleton == null)
            {
                Debug.LogError("Failed to instantiate ClientSingleton prefab.");
                return;
            }
            bool authenticated = false;
            try
            {
                authenticated = await clientSingleton.CreateClient();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception during client authentication: {ex}");
                // Optionally, destroy the clientSingleton if needed
                Destroy(clientSingleton.gameObject);
                return;
            }

            if (authenticated)
            {
                if (clientSingleton.GameManager != null)
                {
                    clientSingleton.GameManager.GoToMenu();
                }
                else
                {
                    Debug.LogError("GameManager is null after authentication.");
                }
            }
            else
            {
                Debug.LogWarning("Client authentication failed.");
                Destroy(clientSingleton.gameObject);
            }
        }
    }
}

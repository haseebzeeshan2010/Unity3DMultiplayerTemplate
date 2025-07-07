using UnityEngine;
using System.Threading.Tasks;
public class ClientSingleton : MonoBehaviour
{
    private static ClientSingleton instance;

    public ClientGameManager GameManager { get; private set; }
    public static ClientSingleton Instance
    {
        get
        {
            if (instance != null) { return instance; }

            instance = FindAnyObjectByType<ClientSingleton>();
            if (instance == null)
            {
                Debug.LogError("No ClientSingleton found in the scene. Please add one.");
                return null;
            }

            return instance;
        }
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateClient()
    {
        if (GameManager != null)
        {
            Debug.LogWarning("GameManager already exists. Disposing previous instance.");
            GameManager.Dispose();
        }
        GameManager = new ClientGameManager();
        bool result = false;
        try
        {
            result = await GameManager.InitAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during client initialization: {ex}");
            GameManager.Dispose();
            GameManager = null;
            return false;
        }
        if (!result)
        {
            Debug.LogWarning("ClientGameManager initialization failed.");
            GameManager.Dispose();
            GameManager = null;
        }
        return result;
    }

    private void OnDestroy()
    {
        if (GameManager != null)
        {
            GameManager.Dispose();
            GameManager = null;
        }
        if (instance == this)
        {
            instance = null;
        }
    }
}

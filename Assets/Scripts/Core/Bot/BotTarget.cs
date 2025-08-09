using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class BotTarget : NetworkBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float updateRate = 0.1f; // How often to update destination

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private NavMeshAgent navMeshAgent;
    private float lastUpdateTime;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Get NavMeshAgent component
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            Debug.LogError($"NavMeshAgent component missing on {gameObject.name}");
            return;
        }

        // Only the server/host should control bot movement
        if (!IsServer)
        {
            navMeshAgent.enabled = false;
            return;
        }

        // Initial setup
        if (target != null)
        {
            SetDestination();
        }
    }

    void FixedUpdate()
    {
        // Only update on server
        if (!IsServer || navMeshAgent == null || target == null)
            return;

        // Update destination at specified rate
        if (Time.time - lastUpdateTime >= updateRate)
        {
            UpdateFollowBehavior();
            lastUpdateTime = Time.time;
        }

        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    private void UpdateFollowBehavior()
    {
        // Always follow the target - no distance check
        SetDestination();
    }

    private void SetDestination()
    {
        if (navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(target.position);
        }
        else
        {
            Debug.LogWarning($"Bot {gameObject.name} is not on NavMesh!");
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (IsServer && navMeshAgent != null && target != null)
        {
            SetDestination();
        }
    }

    private void DrawDebugInfo()
    {
        if (target != null)
        {
            Debug.DrawLine(transform.position, target.position, Color.red);

            if (navMeshAgent.hasPath)
            {
                var path = navMeshAgent.path;
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.blue);
                }
            }
        }
    }

    private void OnValidate()
    {
        // Clamp values in inspector
        updateRate = Mathf.Max(0.01f, updateRate);
    }
}

using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class AxisOscillator : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Oscillation Settings")]
    public Axis oscillationAxis = Axis.X;
    public float amplitude = 1f;         // Distance from center to peak
    public float frequency = 1f;         // Oscillations per second
    public Vector3 centerPosition;       // Oscillation center position

    [Header("Networking")]
    [SerializeField] private AnticipatedNetworkTransform anticipatedNetworkTransform;

    private NetworkObject networkObject;

    private void Start()
    {
        // Try to get the NetworkObject component.
        networkObject = GetComponent<NetworkObject>();

        // If a NetworkObject exists and this client is not the owner, disable the script.
        if (networkObject != null && !networkObject.IsOwner)
        {
            this.enabled = false;
        }
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        Vector3 newPosition = centerPosition;

        switch (oscillationAxis)
        {
            case Axis.X:
                newPosition.x += offset;
                break;
            case Axis.Y:
                newPosition.y += offset;
                break;
            case Axis.Z:
                newPosition.z += offset;
                break;
        }

        if (anticipatedNetworkTransform != null)
        {
            anticipatedNetworkTransform.AnticipateMove(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
    }
}

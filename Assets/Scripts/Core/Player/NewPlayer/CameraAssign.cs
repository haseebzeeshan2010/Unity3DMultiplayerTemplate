using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class CameraAssign : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Settings")]
    [SerializeField] private int ownerPriority = 15;

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
        }
    }
}

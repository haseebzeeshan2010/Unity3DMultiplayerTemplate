using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Collections;
using System;

public class TankPlayer : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private SpriteRenderer minimapIconRenderer; // Reference to the minimap icon
    

    [field: SerializeField] public Health Health { get; private set; } // Reference to the health component

    [field: SerializeField] public CoinWallet Wallet { get; private set; } // Reference to the coin wallet component



    [Header("Settings")]
    [SerializeField] private int ownerPriority = 15;
    [SerializeField] private Color ownerColour; // Color of the minimap icon
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

    public static event Action<TankPlayer> OnPlayerSpawned; // Invoked when a player spawns
    public static event Action<TankPlayer> OnPlayerDespawned; // Invoked when a player despawns

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            UserData userData = 
                HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            
            PlayerName.Value = userData.username;

            OnPlayerSpawned?.Invoke(this); // Broadcast the onplayerspawned event when player spawns
        }


        if(IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
            minimapIconRenderer.color = ownerColour; // Set the color of the minimap icon for the owner
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer)
        {
            OnPlayerDespawned?.Invoke(this); // Broadcast the onplayerdespawned event when player despawns
        }
    }

}

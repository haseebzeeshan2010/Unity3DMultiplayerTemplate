using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Collections;
using System;
public class Player : NetworkBehaviour
{
    [Header("References")]
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

    public static event Action<Player> OnPlayerSpawned; // Invoked when a player spawns
    public static event Action<Player> OnPlayerDespawned; // Invoked when a player despawns

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            PlayerName.Value = userData.username;

            OnPlayerSpawned?.Invoke(this); // Broadcast the onplayerspawned event when player spawns
        }
    }

    public override void OnNetworkDespawn()
    {
        // Handle any cleanup or state reset when the player despawns

        OnPlayerDespawned?.Invoke(this); // Broadcast the onplayerdespawned event when player despawns
    }
}

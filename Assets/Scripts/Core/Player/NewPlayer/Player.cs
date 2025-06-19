using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;
public class Player : NetworkBehaviour
{
    [Header("References")]
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

    public static event Action<Player> OnPlayerSpawned; // Invoked when a player spawns
    public static event Action<Player> OnPlayerDespawned; // Invoked when a player despawns

    public NetworkVariable<bool> Tagged = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> Taggable = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float tagCooldown = 15f;
    [SerializeField] private GameObject TagBlock;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Tagged.Value)
        {
            TagBlock.SetActive(true);
        }
        else
        {
            TagBlock.SetActive(false);
        }

        // Timer logic for tag cooldown (no coroutines)
        // Timer logic for tag cooldown (no coroutines)
        if (!Taggable.Value)
        {
            tagCooldown -= Time.deltaTime;
            if (tagCooldown <= 0f)
            {

                tagCooldown = 5; // Reset cooldown for next time

                if (IsServer)
                {
                    Taggable.Value = true;
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        // if (IsOwner)
        // {
        //     Tagged.Value = false;
        //     Taggable.Value = true;
        // }

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


    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return; // Only handle collisions for the owner

        if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<Player>(out Player player))
        {
            

            if (player == this) return; // Ignore self-collision

            // Only request tag transfer if the other player is tagged and this player is not
            if (!Tagged.Value && player.Tagged.Value)
            {
                RequestTagTransferServerRpc(player.NetworkObjectId, NetworkObjectId);
            }
        }
    }



    


    [ServerRpc]
    private void RequestTagTransferServerRpc(ulong fromPlayerId, ulong toPlayerId)
    {
        // Only the server executes this
        Player fromPlayer = FindPlayerByNetworkObjectId(fromPlayerId);
        Player toPlayer = FindPlayerByNetworkObjectId(toPlayerId);

        if (fromPlayer != null && toPlayer != null && fromPlayer.Tagged.Value && !toPlayer.Tagged.Value && fromPlayer.Taggable.Value && toPlayer.Taggable.Value)
        {
            fromPlayer.Tagged.Value = false;
            toPlayer.Tagged.Value = true;

            fromPlayer.Taggable.Value = false; // Disable taggable state for the previous tagged player
            toPlayer.Taggable.Value = false; // Disable taggable state for the new tagged player
        }
    }

    private Player FindPlayerByNetworkObjectId(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            return networkObject.GetComponent<Player>();
        }
        return null;
    }



}

using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;

public class Player : NetworkBehaviour
{
    [Header("References")]
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public static event Action<Player> OnPlayerSpawned;
    public static event Action<Player> OnPlayerDespawned;

    public enum TagState : byte
    {
        None,
        Tagged,
        Taggable
    }

    public NetworkVariable<TagState> TagStatus = new NetworkVariable<TagState>(
        TagState.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float tagCooldown = 2f;
    [SerializeField] private GameObject TagBlock;

    private void Awake()
    {
        TagStatus.OnValueChanged += OnTagStatusChanged;
    }

    public override void OnDestroy()
    {
        TagStatus.OnValueChanged -= OnTagStatusChanged;
        base.OnDestroy();
    }

    private void OnTagStatusChanged(TagState previous, TagState current)
    {
        TagBlock.SetActive(current == TagState.Tagged);
    }

    void Start()
    {
        TagBlock.SetActive(TagStatus.Value == TagState.Tagged);
    }

    void Update()
    {
        if (!IsServer)
            return;

        if (TagStatus.Value == TagState.None)
        {
            tagCooldown -= Time.deltaTime;
            if (tagCooldown <= 0f)
            {
                tagCooldown = 2f;
                TagStatus.Value = TagState.Taggable;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            PlayerName.Value = userData.username;
            OnPlayerSpawned?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        OnPlayerDespawned?.Invoke(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<Player>(out Player player))
        {
            if (player == this) return;

            if (TagStatus.Value != TagState.Tagged && player.TagStatus.Value == TagState.Tagged)
            {
                RequestTagTransferServerRpc(player.NetworkObjectId, NetworkObjectId);
            }
        }
    }

    [ServerRpc]
    private void RequestTagTransferServerRpc(ulong fromPlayerId, ulong toPlayerId)
    {
        Player fromPlayer = FindPlayerByNetworkObjectId(fromPlayerId);
        Player toPlayer = FindPlayerByNetworkObjectId(toPlayerId);

        if (fromPlayer != null && toPlayer != null && fromPlayer.TagStatus.Value == TagState.Tagged && toPlayer.TagStatus.Value == TagState.Taggable)
        {
            fromPlayer.TagStatus.Value = TagState.None;
            toPlayer.TagStatus.Value = TagState.Tagged;
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

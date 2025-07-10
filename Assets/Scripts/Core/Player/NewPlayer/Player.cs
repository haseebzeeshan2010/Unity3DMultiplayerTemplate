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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            PlayerName.Value = userData.username;
            OnPlayerSpawned?.Invoke(this);
        }
        TagBlock.SetActive(TagStatus.Value == TagState.Tagged);
    }

    public override void OnNetworkDespawn()
    {
        TagStatus.OnValueChanged -= OnTagStatusChanged;
        OnPlayerDespawned?.Invoke(this);
    }

    private void OnTagStatusChanged(TagState previous, TagState current)
    {
        TagBlock.SetActive(current == TagState.Tagged);
    }

    private void Update()
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

    // Server-authoritative tag transfer
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<Player>(out Player otherPlayer))
        {
            if (otherPlayer == this) return;

            // Only allow transfer if this player is taggable and the other is tagged
            if (TagStatus.Value == TagState.Taggable && otherPlayer.TagStatus.Value == TagState.Tagged)
            {
                otherPlayer.TagStatus.Value = TagState.None; // Start cooldown for previous tagger
                TagStatus.Value = TagState.Tagged;
            }
        }
    }
}
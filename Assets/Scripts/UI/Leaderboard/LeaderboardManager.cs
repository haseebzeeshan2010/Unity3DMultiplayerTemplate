using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;
using System.Collections.Generic;

[Serializable]
public struct LeaderboardEntry : INetworkSerializable, IEquatable<LeaderboardEntry>
{
    public ulong clientId;
    public FixedString32Bytes playerName;
    public float taggedTime;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref taggedTime);
    }
    public bool Equals(LeaderboardEntry other) => clientId == other.clientId;
}

public class LeaderboardManager : NetworkBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [SerializeField] private RectTransform leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;

    private NetworkList<LeaderboardEntry> leaderboardList;
    private Dictionary<ulong, GameObject> entryPrefabs = new();

    void Awake()
    {
        Instance = this;
        leaderboardList = new NetworkList<LeaderboardEntry>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Optionally: populate initial list here
        }
        leaderboardList.OnListChanged += OnLeaderboardChanged;
    }

    public override void OnNetworkDespawn()
    {
        leaderboardList.OnListChanged -= OnLeaderboardChanged;
    }

    // Called by TagCounter (via RPC or direct if server)
    [ServerRpc(RequireOwnership = false)]
    public void UpdateTaggedTimeServerRpc(ulong clientId, float taggedTime)
    {
        for (int i = 0; i < leaderboardList.Count; i++)
        {
            if (leaderboardList[i].clientId == clientId)
            {
                var entry = leaderboardList[i];
                entry.taggedTime = taggedTime;
                leaderboardList[i] = entry;
                SortLeaderboard();
                return;
            }
        }
        // New player
        var player = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(clientId);
        leaderboardList.Add(new LeaderboardEntry
        {
            clientId = clientId,
            playerName = player.username,
            taggedTime = taggedTime
        });
        SortLeaderboard();
    }

    private void SortLeaderboard()
    {
        var sorted = new List<LeaderboardEntry>();
        foreach (var entry in leaderboardList)
            sorted.Add(entry);
        sorted.Sort((a, b) => b.taggedTime.CompareTo(a.taggedTime));
        leaderboardList.Clear();
        foreach (var entry in sorted)
            leaderboardList.Add(entry);
    }

    private void OnLeaderboardChanged(NetworkListEvent<LeaderboardEntry> change)
    {
        // Rebuild UI (simple approach)
        foreach (Transform child in leaderboardContainer)
            Destroy(child.gameObject);

        foreach (var entry in leaderboardList)
        {
            var go = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
            go.GetComponent<LeaderboardEntryUI>().Set(entry.playerName.ToString(), entry.taggedTime);
        }
    }
}
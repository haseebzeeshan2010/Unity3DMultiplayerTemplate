using Unity.Collections;
using System;
using Unity.Netcode;

public struct LeaderboardEntityState : INetworkSerializable, IEquatable<LeaderboardEntityState>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public int TagTimed;

    

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref TagTimed);
    }


    public bool Equals(LeaderboardEntityState other)
    {
        return ClientId == other.ClientId && PlayerName.Equals(other.PlayerName) && TagTimed == other.TagTimed;
    }

}
﻿using DarkRift;
using UnityEngine;

public enum MessageTag : ushort {
    LoginRequest = 0,
    LoginAccepted = 1,
    LoginDenied = 2,

    JoinRoomRequest = 100,
    JoinRoomAccepted = 101,
    JoinRoomDenied = 102,

    GameInput = 200,
    GameUpdate = 201,
    StartGameRequest = 202,
    StartGameResponse = 203,
    
    RoundStart = 204,
    RoundEnd = 205,
    StartRoundRequest = 206,

    BulletRequest = 300,
    BulletResponse = 301,
    BulletUpdate = 302,
    BulletInput = 303,
}

public struct LoginRequestData : IDarkRiftSerializable {
    public string UserName;

    public LoginRequestData(string userName) {
        UserName = userName;
    }

    public void Deserialize(DeserializeEvent e) {
        UserName = e.Reader.ReadString();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(UserName);
    }
}

public struct LoginResponseData : IDarkRiftSerializable {
    public ushort ID;
    public LobbyData LobbyData;

    public LoginResponseData(ushort id, LobbyData lobbyData) {
        ID = id;
        LobbyData = lobbyData;
    }

    public void Deserialize(DeserializeEvent e) {
        ID = e.Reader.ReadUInt16();
        LobbyData = e.Reader.ReadSerializable<LobbyData>();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(ID);
        e.Writer.Write(LobbyData);
    }
}

public struct RoomData : IDarkRiftSerializable {
    public string Name;
    public byte Slots;
    public byte MaxSlots;

    public RoomData(string roomName, byte slots, byte maxSlots) {
        Name = roomName;
        Slots = slots;
        MaxSlots = maxSlots;
    }

    public void Deserialize(DeserializeEvent e) {
        Name = e.Reader.ReadString();
        Slots = e.Reader.ReadByte();
        MaxSlots = e.Reader.ReadByte();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Name);
        e.Writer.Write(Slots);
        e.Writer.Write(MaxSlots);
    }
}

public struct LobbyData : IDarkRiftSerializable {
    public RoomData[] Rooms;

    public LobbyData(RoomData[] rooms) {
        Rooms = rooms;
    }

    public void Deserialize(DeserializeEvent e) {
        Rooms = e.Reader.ReadSerializables<RoomData>();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Rooms);
    }
}

public struct JoinRoomRequestData : IDarkRiftSerializable {
    public string RoomName;

    public JoinRoomRequestData(string roomName) {
        RoomName = roomName;
    }
    public void Deserialize(DeserializeEvent e) {
        RoomName = e.Reader.ReadString();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(RoomName);
    }
}

public struct PlayerInputData : IDarkRiftSerializable {
    public bool[] Inputs;// 0 = leftClick, 1 = rightClick
    public Vector2 MovementAxes;
    public Vector2 RotationAxes;
    public uint InputTick;

    public PlayerInputData(bool[] inputs, Vector2 movementAxes, Vector2 rotationAxes, uint inputTick) {
        Inputs = inputs;
        MovementAxes = movementAxes;
        RotationAxes = rotationAxes;
        InputTick = inputTick;
    }

    public void Deserialize(DeserializeEvent e) {
        Inputs = e.Reader.ReadBooleans();
        MovementAxes = new Vector2(e.Reader.ReadSingle(), e.Reader.ReadSingle());
        RotationAxes = new Vector2(e.Reader.ReadSingle(), e.Reader.ReadSingle());
        InputTick = e.Reader.ReadUInt32();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Inputs);
        e.Writer.Write(MovementAxes.x);
        e.Writer.Write(MovementAxes.y);
        e.Writer.Write(RotationAxes.x);
        e.Writer.Write(RotationAxes.y);
        e.Writer.Write(InputTick);
    }
}

public struct PlayerStateData : IDarkRiftSerializable {

    public ushort Id;
    public uint InputTick;
    public Vector3 Position;
    public Quaternion Rotation;

    public PlayerStateData(ushort id, uint inputTick, Vector3 position, Quaternion rotation) {
        Id = id;
        InputTick = inputTick;
        Position = position;
        Rotation = rotation;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
        InputTick = e.Reader.ReadUInt32();
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        Rotation = e.Reader.ReadQuaternion();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
        e.Writer.Write(InputTick);
        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);
        e.Writer.WriteQuaternion(Rotation);
    }
}

public struct PlayerSpawnData : IDarkRiftSerializable {
    public ushort Id;
    public ushort PrefabIndex;
    public string Name;
    public Vector3 Position;

    public PlayerSpawnData(ushort id, ushort prefabIndex, string name, Vector3 position) {
        Id = id;
        PrefabIndex = prefabIndex;
        Name = name;
        Position = position;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
        PrefabIndex = e.Reader.ReadUInt16();
        Name = e.Reader.ReadString();
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
        e.Writer.Write(PrefabIndex);
        e.Writer.Write(Name);
        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);
    }
}

public struct PlayerDespawnData : IDarkRiftSerializable {
    public ushort Id;

    public PlayerDespawnData(ushort id) {
        Id = id;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
    }
}

public struct GameUpdateData : IDarkRiftSerializable {

    public uint Frame;
    public PlayerStateData[] PlayerStates;
    public PlayerDespawnData[] DespawnData;
    public BulletStateData[] BulletStates;
    public BulletDespawnData[] BulletDespawns;

    public GameUpdateData(
        uint frame, 
        PlayerStateData[] playerStates, 
        PlayerDespawnData[] despawnData,
        BulletStateData[] bulletStates,
        BulletDespawnData[] bulletDespawns) {
        
        Frame = frame;
        PlayerStates = playerStates;
        DespawnData = despawnData;
        BulletStates = bulletStates;
        BulletDespawns = bulletDespawns;
    }

    public void Deserialize(DeserializeEvent e) {
        Frame = e.Reader.ReadUInt32();
        PlayerStates = e.Reader.ReadSerializables<PlayerStateData>();
        DespawnData = e.Reader.ReadSerializables<PlayerDespawnData>();
        BulletStates = e.Reader.ReadSerializables<BulletStateData>();
        BulletDespawns = e.Reader.ReadSerializables<BulletDespawnData>();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Frame);
        e.Writer.Write(PlayerStates);
        e.Writer.Write(DespawnData);
        e.Writer.Write(BulletStates);
        e.Writer.Write(BulletDespawns);
    }
}

public struct GameStartData : IDarkRiftSerializable {

    public uint ServerTick;
    public int Seed;
    public PlayerSpawnData[] Players;

    public GameStartData(uint serverTick, int seed, PlayerSpawnData[] players) {
        ServerTick = serverTick;
        Seed = seed;
        Players = players;
    }

    public void Deserialize(DeserializeEvent e) {
        ServerTick = e.Reader.ReadUInt32();
        Seed = e.Reader.ReadInt32();
        Players = e.Reader.ReadSerializables<PlayerSpawnData>();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(ServerTick);
        e.Writer.Write(Seed);
        e.Writer.Write(Players);
    }
}

public struct BulletStateData : IDarkRiftSerializable {
    public ushort Id;
    public ushort PlayerId;
    public uint InputTick;
    public Vector3 Position;

    public BulletStateData(ushort id, ushort playerId, uint inputTick, Vector3 position) {
        Id = id;
        PlayerId = playerId;
        InputTick = inputTick;
        Position = position;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
        PlayerId = e.Reader.ReadUInt16();
        InputTick = e.Reader.ReadUInt32();
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
        e.Writer.Write(PlayerId);
        e.Writer.Write(InputTick);
        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);
    }
}

public struct BulletSpawnData : IDarkRiftSerializable {

    public ushort Id;
    public ushort PlayerId;
    public Vector3 Position;
    public Vector3 Velocity;

    public BulletSpawnData(ushort id, ushort playerId, Vector3 position, Vector3 velocity) {
        Id = id;
        PlayerId = playerId;
        Position = position;
        Velocity = velocity;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
        PlayerId = e.Reader.ReadUInt16();
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        Velocity = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
        e.Writer.Write(PlayerId);

        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);

        e.Writer.Write(Velocity.x);
        e.Writer.Write(Velocity.y);
        e.Writer.Write(Velocity.z);
    }
}

public struct BulletDespawnData : IDarkRiftSerializable {
    public ushort Id;

    public BulletDespawnData(ushort id) {
        Id = id;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
    }
}

public struct BulletRequestData : IDarkRiftSerializable {

    public ushort PlayerId;
    public uint InputTick;

    public BulletRequestData(ushort playerId, uint inputTick) {
        PlayerId = playerId;
        InputTick = inputTick;
    }

    public void Deserialize(DeserializeEvent e) {
        PlayerId = e.Reader.ReadUInt16();
        InputTick = e.Reader.ReadUInt32();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(PlayerId);
        e.Writer.Write(InputTick);
    }
}

public struct BulletResponseData : IDarkRiftSerializable {

    public ushort PlayerId;
    public ushort BulletId;
    
    public BulletResponseData(ushort playerId, ushort bulletId) {
        PlayerId = playerId;
        BulletId = bulletId;
    }

    public void Deserialize(DeserializeEvent e) {
        PlayerId = e.Reader.ReadUInt16();
        BulletId = e.Reader.ReadUInt16();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(PlayerId);
        e.Writer.Write(BulletId);
    }
}

public struct BulletUpdateData : IDarkRiftSerializable {
    public BulletStateData[] BulletStates;

    public BulletUpdateData(BulletStateData[] bulletStates) {
        BulletStates = bulletStates;
    }

    public void Deserialize(DeserializeEvent e) {
        BulletStates = e.Reader.ReadSerializables<BulletStateData>();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(BulletStates);
    }
}

public struct BulletInputData : IDarkRiftSerializable {

    public ushort Id;
    public uint InputTick;
    public Vector2 MovementAxes;

    public BulletInputData(ushort id, uint inputTick, Vector2 movementAxes) {
        Id = id;
        InputTick = inputTick;
        MovementAxes = movementAxes;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
        InputTick = e.Reader.ReadUInt32();
        MovementAxes = new Vector2(e.Reader.ReadSingle(), e.Reader.ReadSingle());
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
        e.Writer.Write(InputTick);
        e.Writer.Write(MovementAxes.x);
        e.Writer.Write(MovementAxes.y);
    }
}

public struct RoundStartData : IDarkRiftSerializable {

    public PlayerSpawnData[] Players;

    public RoundStartData(PlayerSpawnData[] players) {
        Players = players;
    }

    public void Deserialize(DeserializeEvent e) {
        Players = e.Reader.ReadSerializables<PlayerSpawnData>();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Players);
    }
}

public struct RoundEndData : IDarkRiftSerializable {

    public ushort WinnerId;
    public byte RoundsLeft;
    public BulletDespawnData[] BulletDespawns;

    public RoundEndData(ushort winnerId, byte roundsLeft, BulletDespawnData[] bulletDespawns) {
        WinnerId = winnerId;
        RoundsLeft = roundsLeft;
        BulletDespawns = bulletDespawns;
    }

    public void Deserialize(DeserializeEvent e) {
        WinnerId = e.Reader.ReadUInt16();
        RoundsLeft = e.Reader.ReadByte();
        BulletDespawns = e.Reader.ReadSerializables<BulletDespawnData>();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(WinnerId);
        e.Writer.Write(RoundsLeft);
        e.Writer.Write(BulletDespawns);
    }
}
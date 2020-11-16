using DarkRift;
using UnityEngine;

public enum MessageTag : ushort {
    LoginRequest = 0,
    LoginAccepted = 1,
    LoginDenied = 2,

    JoinRoomRequest = 100,
    JoinRoomAccepted = 101,
    JoinRoomDenied = 102,

    GameInput = 200,
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
    public uint Time;

    public PlayerInputData(bool[] inputs, Vector2 movementAxes, Vector2 rotationAxes, uint time) {
        Inputs = inputs;
        MovementAxes = movementAxes;
        RotationAxes = rotationAxes;
        Time = time;
    }

    public void Deserialize(DeserializeEvent e) {
        Inputs = e.Reader.ReadBytesAsBools(Inputs.Length);
        MovementAxes = new Vector2(e.Reader.ReadSingle(), e.Reader.ReadSingle());
        RotationAxes = new Vector2(e.Reader.ReadSingle(), e.Reader.ReadSingle());
        Time = e.Reader.ReadUInt16();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.WriteBoolsAsBytes(Inputs);
        e.Writer.Write(MovementAxes.x);
        e.Writer.Write(MovementAxes.y);
        e.Writer.Write(RotationAxes.x);
        e.Writer.Write(RotationAxes.y);
        e.Writer.Write(Time);
    }
}

public struct PlayerStateData : IDarkRiftSerializable {

    public ushort Id;
    public Vector3 Position;
    public Quaternion Rotation;

    public PlayerStateData(ushort id, Vector3 position, Quaternion rotation) {
        Id = id;
        Position = position;
        Rotation = rotation;
    }

    public void Deserialize(DeserializeEvent e) {
        Id = e.Reader.ReadUInt16();
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        Rotation = e.Reader.ReadQuaternion();
    }

    public void Serialize(SerializeEvent e) {
        e.Writer.Write(Id);
        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);
        e.Writer.WriteQuaternion(Rotation);
    }
}
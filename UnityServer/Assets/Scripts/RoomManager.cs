using DarkRift.Server;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour {

    public static RoomManager Instance { get; private set; }

    [SerializeField] private GameObject roomPrefab;

    private readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();

    public void CreateRoom(string name, byte slots, byte rounds) {

        var go = Instantiate(roomPrefab);
        var room = go.GetComponent<Room>();

        room.Initialize(name, slots, rounds);

        rooms.Add(name, room);
    }

    public void RemoveRoom(string name) {
        var room = rooms[name];
        room.Close();
        rooms.Remove(name);
    }

    public RoomData[] GetRoomData() {
        var r = new RoomData[rooms.Count];
        var i = 0;
        
        foreach (var kvp in rooms) {
            var room = kvp.Value;
            r[i++] = new RoomData(room.RoomName, (byte)room.ClientConnections.Count, room.Slots);
        }

        return r;
    }

    public void TryJoinRoom(IClient client, JoinRoomRequestData requestData) {
        var canJoin = ServerManager.Instance.Players.TryGetValue(client.ID, out var clientConnection);
        
        if (!rooms.TryGetValue(requestData.RoomName, out var room)) {
            canJoin = false;
        } 
        
        if (room.OpenSlots < 1) {
            canJoin = false;
        }

        if (canJoin) {
            room.AddPlayer(clientConnection);
        } else {
            using (var msg = DarkRift.Message.Create((ushort)MessageTag.JoinRoomDenied, new LobbyData(GetRoomData()))) {
                client.SendMessage(msg, DarkRift.SendMode.Reliable);
            }
        }
    }

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateRoom("DebugRoom", 1, 4);
        //CreateRoom("TestRoom", 2, 0);
    }

}

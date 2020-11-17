using DarkRift;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Room : MonoBehaviour {

    public string RoomName => roomName;
    public byte Slots => slots;

    public uint ServerTick { get; private set; }

    public List<ClientConnection> ClientConnections { get; } = new List<ClientConnection>();

    [SerializeField] private string roomName;
    [SerializeField] private byte slots;
    [SerializeField] private GameObject playerPrefab;
    
    private Scene scene;
    private PhysicsScene physicsScene;

    private readonly List<ServerPlayer> serverPlayers = new List<ServerPlayer>();
    private readonly List<PlayerStateData> playerStates = new List<PlayerStateData>();
    private readonly List<PlayerSpawnData> spawnData = new List<PlayerSpawnData>();
    private readonly List<PlayerDespawnData> despawnData = new List<PlayerDespawnData>();

    public void Initialize(string roomName, byte slots) {
        this.roomName = roomName;
        this.slots = slots;

        var csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scene = SceneManager.CreateScene($"room_{roomName}", csp);
        physicsScene = scene.GetPhysicsScene();

        SceneManager.MoveGameObjectToScene(gameObject, scene);
    }

    public void AddPlayer(ClientConnection clientConnection) {
        if (ClientConnections.Contains(clientConnection)) {
            return;
        }

        clientConnection.Room = this;
        ClientConnections.Add(clientConnection);

        using (var msg = Message.CreateEmpty((ushort)MessageTag.JoinRoomAccepted)) {
            clientConnection.client.SendMessage(msg, SendMode.Reliable);
        }
    }

    public void SpawnPlayer(ClientConnection clientConnection) {
        var go = Instantiate(playerPrefab, transform);
        var serverPlayer = go.GetComponent<ServerPlayer>();

        serverPlayer.Initialize(Vector3.zero, clientConnection);

        serverPlayers.Add(serverPlayer);
        playerStates.Add(default);
        spawnData.Add(serverPlayer.GetSpawnData());
    }

    public void RemovePlayer(ClientConnection clientConnection) {
        clientConnection.Room = null;
        ClientConnections.Remove(clientConnection);
        serverPlayers.Remove(clientConnection.ServerPlayer);
        despawnData.Add(new PlayerDespawnData(clientConnection.client.ID));
        Destroy(clientConnection.ServerPlayer.gameObject);
    }

    public void Close() {
        foreach (var clientConnection in ClientConnections) {
            RemovePlayer(clientConnection);
        }
        Destroy(gameObject);
    }

    public PlayerSpawnData[] GetAllSpawnData() {
        var r = new PlayerSpawnData[serverPlayers.Count];

        for (int i = 0; i < serverPlayers.Count; i++) {
            r[i] = serverPlayers[i].GetSpawnData();
        }

        return r;
    }

    private void FixedUpdate() {
        ServerTick++;

        // player states updaten
        for (int i = 0; i < serverPlayers.Count; i++) {
            var p = serverPlayers[i];
            p.PlayerPreUpdate();
            playerStates[i] = p.PlayerUpdate();
        }

        // updates an alle clients schicken
        var playerStateUpdates = playerStates.ToArray();
        var spawnDataUpdates = spawnData.ToArray();
        var despawnDataUpdates = despawnData.ToArray();

        foreach (var p in serverPlayers) {
            using (var msg = Message.Create((ushort)MessageTag.GameUpdate, new GameUpdateData(ServerTick, playerStateUpdates, spawnDataUpdates, despawnDataUpdates))) {
                p.Client.SendMessage(msg, SendMode.Reliable);
            }
        }

        spawnData.Clear();
        despawnData.Clear();
    }
}

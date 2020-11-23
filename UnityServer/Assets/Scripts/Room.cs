using DarkRift;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GameObjectPool))]
public class Room : MonoBehaviour {

    public string RoomName => roomName;
    public byte Slots => slots;

    public uint ServerTick { get; private set; }

    public GameObjectPool BulletPool { get; private set; }

    public List<ClientConnection> ClientConnections { get; } = new List<ClientConnection>();

    [SerializeField] private string roomName;
    [SerializeField] private byte slots;
    [SerializeField] private GameObject playerPrefab;
    
    private Scene scene;
    private PhysicsScene physicsScene;

    private readonly List<ServerPlayer> serverPlayers = new List<ServerPlayer>();
    private readonly List<PlayerStateData> playerStates = new List<PlayerStateData>();
    private readonly List<PlayerSpawnData> playerSpawns = new List<PlayerSpawnData>();
    private readonly List<PlayerDespawnData> playerDespawns = new List<PlayerDespawnData>();

    private readonly List<ServerBullet> serverBullets = new List<ServerBullet>();
    private readonly Dictionary<ushort, BulletStateData> bulletStates = new Dictionary<ushort, BulletStateData>();
    private readonly List<BulletSpawnData> bulletSpawns = new List<BulletSpawnData>();
    private readonly List<BulletDespawnData> bulletDespawns = new List<BulletDespawnData>();

    private int cntr;

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
        playerStates.Add(serverPlayer.PlayerState);
        playerSpawns.Add(serverPlayer.GetSpawnData());

        var spawnData = GetAllSpawnData();

        using (var msg = Message.Create((ushort)MessageTag.StartGameResponse, new GameStartData(ServerTick, spawnData))) {
            clientConnection.client.SendMessage(msg, SendMode.Reliable);
        }
    }

    public void RemovePlayer(ClientConnection clientConnection) {
        clientConnection.Room = null;
        ClientConnections.Remove(clientConnection);
        serverPlayers.Remove(clientConnection.ServerPlayer);
        playerDespawns.Add(new PlayerDespawnData(clientConnection.client.ID));
        Destroy(clientConnection.ServerPlayer.gameObject);
    }

    public void SpawnBullet(ServerPlayer shooter) {
        var spawnPosition = shooter.GunPointState.Position;
        var direction = shooter.GunPointState.Direction;
        var bullet = BulletPool.Obtain(true);
        var serverBullet = bullet.GetComponent<ServerBullet>();
        var spawnData = new BulletSpawnData((ushort)BulletPool.LastObtainedIndex, shooter.PlayerState.Id, spawnPosition, direction * serverBullet.Speed);

        serverBullet.Initialize(shooter, spawnData);

        serverBullets.Add(serverBullet);
        bulletStates[serverBullet.Id] = serverBullet.BulletState;
        bulletSpawns.Add(spawnData);

        Debug.Log($"spawning bullet {spawnData.Id} at {spawnData.Position} velocity {direction * serverBullet.Speed}");
    }

    public void DespawnBullet(ServerBullet bullet) {
        var idx = serverBullets.IndexOf(bullet);
        BulletPool.Free(bullet.gameObject);
        serverBullets.RemoveAt(idx);
        bulletStates.Remove(bullet.Id);
        bulletDespawns.Add(new BulletDespawnData(bullet.Id));
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

    private void Awake() {
        BulletPool = GetComponent<GameObjectPool>();
    }

    private void FixedUpdate() {
        ServerTick++;

        foreach (var p in serverPlayers) {
            p.PlayerPreUpdate();
        }

        // player states updaten
        for (int i = 0; i < serverPlayers.Count; i++) {
            var p = serverPlayers[i];
            playerStates[i] = p.PlayerUpdate();
        }

        // bullet states updaten
        for (int i = 0; i < serverBullets.Count; i++) {
            var b = serverBullets[i];
            bulletStates[b.Id] = b.BulletUpdate();
        }

        // updates an alle clients schicken
        var playerStateUpdates = playerStates.ToArray();
        var spawnDataUpdates = playerSpawns.ToArray();
        var despawnDataUpdates = playerDespawns.ToArray();

        var bulletStateUpdates = new BulletStateData[bulletStates.Count];
        bulletStates.Values.CopyTo(bulletStateUpdates, 0);

        var bulletSpawnUpdates = bulletSpawns.ToArray();
        var bulletDespawnUpdates = bulletDespawns.ToArray();

        foreach (var p in serverPlayers) {
            
            var updateData = new GameUpdateData(
                p.InputTick,
                playerStateUpdates, spawnDataUpdates, despawnDataUpdates,
                bulletStateUpdates, bulletSpawnUpdates, bulletDespawnUpdates
            );

            using (var msg = Message.Create((ushort)MessageTag.GameUpdate, updateData)) {
                p.Client.SendMessage(msg, SendMode.Reliable);
            }

        }

        // spawnlisten clearen, damit nichts doppelt gespawnt wird
        playerSpawns.Clear();
        playerDespawns.Clear();

        bulletSpawns.Clear();
        bulletDespawns.Clear();
    }
}

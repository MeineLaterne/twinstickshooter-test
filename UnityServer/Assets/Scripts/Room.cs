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
    
    private readonly List<ServerPlayer> serverPlayers = new List<ServerPlayer>();
    private readonly List<PlayerStateData> playerStates = new List<PlayerStateData>();
    private readonly List<PlayerSpawnData> playerSpawns = new List<PlayerSpawnData>();
    private readonly List<PlayerDespawnData> playerDespawns = new List<PlayerDespawnData>();

    private readonly List<ServerBullet> serverBullets = new List<ServerBullet>();
    private readonly List<BulletSpawnData> bulletSpawns = new List<BulletSpawnData>();
    private readonly List<BulletDespawnData> bulletDespawns = new List<BulletDespawnData>();
    private readonly Dictionary<ushort, BulletStateData> bulletStates = new Dictionary<ushort, BulletStateData>();

    private readonly Queue<ServerBullet> requestedBullets = new Queue<ServerBullet>();

    public void Initialize(string roomName, byte slots) {
        this.roomName = roomName;
        this.slots = slots;

        var csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scene = SceneManager.CreateScene($"room_{roomName}", csp);
        
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

    public void OnBulletRequest(BulletRequestData requestData) {
        if (ServerManager.Instance.Players.TryGetValue(requestData.PlayerId, out ClientConnection clientConnection)) {
            
            var bullet = BulletPool.Obtain(true);
            var serverBullet = bullet.GetComponent<ServerBullet>();

            serverBullet.Initialize((ushort)BulletPool.LastObtainedIndex, requestData.PlayerId, clientConnection.ServerPlayer);

            requestedBullets.Enqueue(serverBullet);

            var bulletResponse = new BulletResponseData(requestData.PlayerId, serverBullet.Id);

            foreach (var p in serverPlayers) {
                using (var msg = Message.Create((ushort)MessageTag.BulletResponse, bulletResponse)) {
                    p.Client.SendMessage(msg, SendMode.Reliable);
                }
            }

        }
    }

    public void SpawnBullet(ServerBullet bullet) {
        var spawnPosition = bullet.Owner.GunPointState.Position;
        var direction = bullet.Owner.GunPointState.Direction;
        var spawnData = new BulletSpawnData(bullet.Id, bullet.PlayerId, spawnPosition, direction);

        serverBullets.Add(bullet);

        bulletStates[bullet.Id] = bullet.Go(spawnData);

        Debug.Log($"spawning bullet {spawnData.Id} at {spawnData.Position} direction {direction}");
    }

    public void DespawnBullet(ServerBullet bullet) {
        BulletPool.Free(bullet.gameObject);
        serverBullets.Remove(bullet);
        bulletStates.Remove(bullet.Id);
        bulletDespawns.Add(new BulletDespawnData(bullet.Id));
        Debug.Log($"despawning bullet {bullet.Id} at {bullet.transform.position}");
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

        var bulletDespawnUpdates = bulletDespawns.ToArray();

        foreach (var p in serverPlayers) {
            
            var updateData = new GameUpdateData(
                p.InputTick,
                playerStateUpdates, spawnDataUpdates, despawnDataUpdates,
                bulletStateUpdates, bulletDespawnUpdates
            );

            //Debug.Log($"sending update: {updateData.Frame}");

            using (var msg = Message.Create((ushort)MessageTag.GameUpdate, updateData)) {
                p.Client.SendMessage(msg, SendMode.Reliable);
            }

        }

        // bullets für den nächsten frame initialisieren
        while (requestedBullets.Count > 0) {
            var bullet = requestedBullets.Dequeue();
            SpawnBullet(bullet);
        }

        // spawnlisten clearen, damit nichts doppelt gespawnt wird
        playerSpawns.Clear();
        playerDespawns.Clear();

        bulletDespawns.Clear();
    }

}

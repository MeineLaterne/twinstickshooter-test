using DarkRift;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GameObjectPool))]
public class Room : MonoBehaviour {

    public string RoomName => roomName;
    public byte Slots => slots;
    public int OpenSlots { get; private set; }
    public uint ServerTick { get; private set; }
    public bool IsRunning { get; private set; }

    public List<ClientConnection> ClientConnections { get; } = new List<ClientConnection>();

    [SerializeField] private string roomName;
    [SerializeField] private byte slots;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject[] mapPrefabs;

    private byte rounds;
    private int currentMapIndex;
    private int seed;
    private int[] mapOrder;
    private GameObject currentMap;
    private Scene scene;
    private GameObjectPool bulletPool;
    

    private readonly List<ServerPlayer> serverPlayers = new List<ServerPlayer>();
    private readonly List<PlayerStateData> playerStates = new List<PlayerStateData>();
    private readonly List<PlayerSpawnData> playerSpawns = new List<PlayerSpawnData>();
    private readonly List<PlayerDespawnData> playerDespawns = new List<PlayerDespawnData>();

    private readonly List<ServerBullet> serverBullets = new List<ServerBullet>();
    private readonly List<BulletDespawnData> bulletDespawns = new List<BulletDespawnData>();
    private readonly Dictionary<ushort, BulletStateData> bulletStates = new Dictionary<ushort, BulletStateData>();
    private readonly Queue<Vector3> spawnPoints = new Queue<Vector3>();
    private readonly Queue<ServerBullet> requestedBullets = new Queue<ServerBullet>();

    private bool ready = false;

    public void Initialize(string roomName, byte slots, byte rounds) {
        this.roomName = roomName;
        this.slots = slots;
        this.rounds = rounds;
        OpenSlots = slots;

        seed = (int)(Random.value * int.MaxValue);

        Random.InitState(seed);
        
        mapOrder = new int[rounds];
        currentMapIndex = 0;
        
        for (var i = 0; i < rounds; i++) {
            mapOrder[i] = Mathf.RoundToInt((mapPrefabs.Length - 1) * Random.value);
        }

        var csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scene = SceneManager.CreateScene(roomName, csp);

        currentMap = Instantiate(mapPrefabs[mapOrder[currentMapIndex]]);

        SceneManager.MoveGameObjectToScene(currentMap, scene);
        SceneManager.MoveGameObjectToScene(gameObject, scene);

        QueueSpawnPoints(currentMap.transform.Find("SpawnPoints"));
    }

    public void AddPlayer(ClientConnection clientConnection) {
        if (ClientConnections.Contains(clientConnection)) {
            return;
        }

        clientConnection.Room = this;
        ClientConnections.Add(clientConnection);
        OpenSlots--;

        using (var msg = Message.CreateEmpty((ushort)MessageTag.JoinRoomAccepted)) {
            clientConnection.client.SendMessage(msg, SendMode.Reliable);
        }
    }

    public void RemovePlayer(ClientConnection clientConnection) {
        clientConnection.Room = null;
        ClientConnections.Remove(clientConnection);
        serverPlayers.Remove(clientConnection.ServerPlayer);
        playerDespawns.Add(new PlayerDespawnData(clientConnection.client.ID));
        Destroy(clientConnection.ServerPlayer.gameObject);
        OpenSlots++;
        ready = OpenSlots < 1;
    }

    internal void SpawnPlayer(ClientConnection clientConnection) {
        if (ready) return;

        var go = Instantiate(playerPrefab, transform);
        var serverPlayer = go.GetComponent<ServerPlayer>();
        var playerPos = spawnPoints.Dequeue();

        serverPlayer.Initialize(playerPos, clientConnection);
        
        serverPlayers.Add(serverPlayer);
        playerStates.Add(serverPlayer.PlayerState);
        playerSpawns.Add(serverPlayer.GetSpawnData());

        if (OpenSlots < 1) {
            StartGame();
            ready = true;
        }
    }

    internal void DespawnPlayer(ServerPlayer player) {

        if (serverPlayers.Count - 1 < 2) {
            EndRound();
        }

        if (serverPlayers.Remove(player)) {
            playerStates.Clear();
            player.gameObject.SetActive(false);
        }
    }
    
    internal void OnStartRoundRequest(ClientConnection clientConnection) {
        if (ClientConnections.Contains(clientConnection)) {
            serverPlayers.Add(clientConnection.ServerPlayer);
            
            if (serverPlayers.Count == slots) {
                StartNextRound();
            }
        }
    }

    internal void OnBulletRequest(BulletRequestData requestData) {
        if (ServerManager.Instance.Players.TryGetValue(requestData.PlayerId, out ClientConnection clientConnection)) {
            
            var bullet = bulletPool.Obtain(true);
            var serverBullet = bullet.GetComponent<ServerBullet>();

            serverBullet.Initialize((ushort)bulletPool.LastObtainedIndex, requestData.PlayerId, clientConnection.ServerPlayer);

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

    }

    public void DespawnBullet(ServerBullet bullet) {
        bulletPool.Free(bullet.gameObject);
        serverBullets.Remove(bullet);
        bulletStates.Remove(bullet.Id);
        bulletDespawns.Add(new BulletDespawnData(bullet.Id));
       // Debug.Log($"despawning bullet {bullet.Id} at {bullet.transform.position}");
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

    private void StartNextRound() {

        QueueSpawnPoints(currentMap.transform.Find("SpawnPoints"));

        foreach (var p in serverPlayers) {
            var pos = spawnPoints.Dequeue();
            p.Teleport(pos);
        }

        var spawnData = GetAllSpawnData();
        foreach (var p in serverPlayers) {

            playerStates.Add(p.PlayerState);

            using (var msg = Message.Create((ushort)MessageTag.RoundStart, new RoundStartData(spawnData))) {
                p.Client.SendMessage(msg, SendMode.Reliable);
            }
        }

        IsRunning = true;
    }

    private void EndRound() {

        currentMapIndex++;

        if (currentMapIndex < mapOrder.Length) {
            var previousMap = GameObject.FindGameObjectWithTag("Map");
            Destroy(previousMap);

            currentMap = Instantiate(mapPrefabs[mapOrder[currentMapIndex]]);
            SceneManager.MoveGameObjectToScene(currentMap, scene);

            
        } else {
            // Spiel vorbei
            Debug.Log("game over");
            Close();
        }

        serverPlayers.Clear();
        playerStates.Clear();

        var bullets = GameObject.FindGameObjectsWithTag("Bullet");
        
        foreach (var b in bullets) {
            var sb = b.GetComponent<ServerBullet>();
            if (serverBullets.Contains(sb)) {
                sb.Disable();
            }
        }

        serverBullets.Clear();
        bulletStates.Clear();

        foreach (var cc in ClientConnections) {
            using (var msg = Message.Create((ushort)MessageTag.RoundEnd, new RoundEndData(0, 0, bulletDespawns.ToArray()))) {
                cc.client.SendMessage(msg, SendMode.Reliable);
            }
        }

        IsRunning = false;
    }

    private void StartGame() {
        var spawnData = GetAllSpawnData();

        foreach (var p in serverPlayers) {
            using (var msg = Message.Create((ushort)MessageTag.StartGameResponse, new GameStartData(ServerTick, seed, spawnData))) {
                p.Client.SendMessage(msg, SendMode.Reliable);
            }
        }

        IsRunning = true;
    }

    private void QueueSpawnPoints(Transform spawnPointContainer) {
        Debug.Log($"fetching spawnpoints from {spawnPointContainer.gameObject.name}");
        for (int i = 0; i < spawnPointContainer.childCount; i++) {
            var pos = spawnPointContainer.GetChild(i).position;
            spawnPoints.Enqueue(pos);
        }
    }

    private void Awake() {
        bulletPool = GetComponent<GameObjectPool>();
    }

    private void FixedUpdate() {
        if (!IsRunning) return;

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
        var despawnDataUpdates = playerDespawns.ToArray();

        var bulletStateUpdates = new BulletStateData[bulletStates.Count];
        bulletStates.Values.CopyTo(bulletStateUpdates, 0);

        var bulletDespawnUpdates = bulletDespawns.ToArray();

        foreach (var p in serverPlayers) {
            
            var updateData = new GameUpdateData(
                p.InputTick,
                playerStateUpdates, despawnDataUpdates,
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
        
        playerDespawns.Clear();

        bulletDespawns.Clear();
    }

}

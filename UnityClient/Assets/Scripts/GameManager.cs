using DarkRift;
using DarkRift.Client;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameObjectPool))]
public class GameManager : MonoBehaviour {
    
    public static GameManager Instance { get; private set; }
    
    public uint ClientTick { get; private set; }
    public uint LastServerTick { get; private set; }

    [SerializeField] private GameObject[] playerPrefabs;

    private GameObjectPool bulletPool;

    private readonly Dictionary<ushort, ClientPlayer> players = new Dictionary<ushort, ClientPlayer>();
    private readonly Dictionary<ushort, ClientBullet> activeBullets = new Dictionary<ushort, ClientBullet>();

    private readonly Queue<BulletSpawnData> bulletSpawns = new Queue<BulletSpawnData>();
    private readonly QueueBuffer<GameUpdateData> buffer = new QueueBuffer<GameUpdateData>(1);

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        bulletPool = GetComponent<GameObjectPool>();

        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        ConnectionManager.Instance.Client.MessageReceived += OnMessage;
        using (var msg = Message.CreateEmpty((ushort)MessageTag.StartGameRequest)) {
            ConnectionManager.Instance.Client.SendMessage(msg, SendMode.Reliable);
        }
    }

    private void OnDestroy() {
        Instance = null;
        ConnectionManager.Instance.Client.MessageReceived -= OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e) {
        using (var msg = e.GetMessage()) {
            switch ((MessageTag)msg.Tag) {
                case MessageTag.GameUpdate:
                    OnGameUpdate(msg.Deserialize<GameUpdateData>());
                    break;

                case MessageTag.BulletResponse:
                    OnBulletResponse(msg.Deserialize<BulletResponseData>());
                    break;

                case MessageTag.StartGameResponse:
                    OnGameStart(msg.Deserialize<GameStartData>());
                    break;

            }
        }
    }

    private void OnGameStart(GameStartData startData) {
        LastServerTick = startData.ServerTick;
        ClientTick = startData.ServerTick;

        Debug.Log($"starting game with {startData.Players.Length} players");

        foreach (var spawnData in startData.Players) {
            SpawnPlayer(spawnData);
        }
    }

    private void OnGameUpdate(GameUpdateData updateData) => buffer.Add(updateData);

    private void FixedUpdate() {
        //Debug.Log($"GameManager:FixedUpdate {ClientTick}");
        ClientTick++;
        var dataToProcess = buffer.Get();
        //Debug.Log($"processing {dataToProcess.Length} updates");
        foreach (var data in dataToProcess) {
            UpdateClientGameState(data);
        }

        while (bulletSpawns.Count > 0) {
            var bullet = bulletSpawns.Dequeue();
            SpawnBullet(bullet);
        }
    }

    private void UpdateClientGameState(GameUpdateData updateData) {
        LastServerTick = updateData.Frame;

        // despawn
        foreach (var despawnData in updateData.DespawnData) {
            if (players.ContainsKey(despawnData.Id)) {
                Destroy(players[despawnData.Id].gameObject);
                players.Remove(despawnData.Id);
            }
        }

        foreach (var bulletDespawnData in updateData.BulletDespawns) {
            DespawnBullet(bulletDespawnData.Id);
        }
        
        // spawn
        foreach (var spawnData in updateData.SpawnData) {
            if (spawnData.Id != ConnectionManager.Instance.PlayerId) {
                SpawnPlayer(spawnData);
            }
        }

        // update
        foreach (var playerState in updateData.PlayerStates) {
            if (players.TryGetValue(playerState.Id, out ClientPlayer p)) {
                p.UpdatePlayerState(playerState);
            }
        }

        foreach (var bulletState in updateData.BulletStates) {
            if (activeBullets.TryGetValue(bulletState.Id, out ClientBullet b)) {
                b.UpdateBulletState(bulletState);
            }
        }

    }

    private void SpawnPlayer(PlayerSpawnData spawnData) {
        var go = Instantiate(playerPrefabs[spawnData.PrefabIndex]);
        var player = go.GetComponent<ClientPlayer>();
        player.Initialize(spawnData.Id, spawnData.Name);
        players.Add(spawnData.Id, player);
    }

    private void OnBulletResponse(BulletResponseData responseData) {
        if (players.TryGetValue(responseData.PlayerId, out ClientPlayer p)) {
            var spawnPosition = p.GunPoint.position;
            var direction = p.transform.forward;
            var spawnData = new BulletSpawnData(responseData.BulletId, responseData.PlayerId, spawnPosition, direction);

            bulletSpawns.Enqueue(spawnData);
        }

    }

    private void SpawnBullet(BulletSpawnData spawnData) {
        var go = bulletPool.Obtain(true);
        var bullet = go.GetComponent<ClientBullet>();
        if (players.TryGetValue(spawnData.PlayerId, out ClientPlayer p)) {
            spawnData.Position = p.GunPoint.position;
            spawnData.Velocity = p.transform.forward;
            bullet.Initialize(spawnData);
            activeBullets.Add(spawnData.Id, bullet);
            Debug.Log($"spawning bullet: {bullet.Id} at {go.transform.position}\nplayer position: {p.transform.position}");
        }
        
    }

    private void DespawnBullet(ushort bulletId) {
        if (activeBullets.TryGetValue(bulletId, out ClientBullet bullet)) {
            bulletPool.Free(bullet.gameObject);
            activeBullets.Remove(bulletId);
            Debug.Log($"despawning bullet {bulletId} at {bullet.transform.position}");
        }
    }

}

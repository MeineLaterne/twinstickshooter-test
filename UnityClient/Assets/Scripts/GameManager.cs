using DarkRift;
using DarkRift.Client;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameObjectPool))]
public class GameManager : MonoBehaviour {
    
    internal static GameManager Instance { get; private set; }
    internal uint ClientTick { get; private set; }
    internal uint LastServerTick { get; private set; }

    [SerializeField] private GameObject[] playerPrefabs;
    [SerializeField] private GameObject[] mapPrefabs;

    private int rounds = 8;
    private int currentMapIndex;
    private int[] mapOrder;
    private GameObject currentMap;

    private GameObjectPool bulletPool;

    private readonly Dictionary<ushort, ClientPlayer> activePlayers = new Dictionary<ushort, ClientPlayer>();
    private readonly Dictionary<ushort, ClientPlayer> inactivePlayers = new Dictionary<ushort, ClientPlayer>();
    
    private readonly Dictionary<ushort, ClientBullet> activeBullets = new Dictionary<ushort, ClientBullet>();

    private readonly Queue<BulletResponseData> bulletResponses = new Queue<BulletResponseData>();
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

                case MessageTag.RoundStart:
                    OnRoundStart(msg.Deserialize<RoundStartData>());
                    break;
                
                case MessageTag.RoundEnd:
                    OnRoundEnd(msg.Deserialize<RoundEndData>());
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

        // Mapreihenfolge festlegen
        Random.InitState(startData.Seed);
        
        mapOrder = new int[rounds];
        currentMapIndex = 0;

        for (int i = 0; i < rounds; i++) {
            mapOrder[i] = Mathf.RoundToInt((mapPrefabs.Length - 1) * Random.value);
        }

        currentMap = Instantiate(mapPrefabs[mapOrder[currentMapIndex]]);

        foreach (var spawnData in startData.Players) {
            SpawnPlayer(spawnData);
        }
    }

    private void OnGameUpdate(GameUpdateData updateData) => buffer.Add(updateData);

    private void OnRoundStart(RoundStartData roundStartData) {
        foreach (var spawnData in roundStartData.Players) {
            if (inactivePlayers.TryGetValue(spawnData.Id, out ClientPlayer p)) {
                inactivePlayers.Remove(spawnData.Id);
                activePlayers.Add(spawnData.Id, p);
                p.gameObject.SetActive(true);
                p.Teleport(spawnData.Position);
            }
        }
    }

    private void OnRoundEnd(RoundEndData roundEndData) {
        Debug.Log($"round {rounds - roundEndData.RoundsLeft} was won by player {roundEndData.WinnerId}");

        // verbleibende bullets clearen
        foreach (var bulletDespawn in roundEndData.BulletDespawns) {
            DespawnBullet(bulletDespawn.Id);
        }
        
        // nächste map laden
        currentMapIndex++;
        if (currentMapIndex < mapOrder.Length) {
            var previousMap = GameObject.FindGameObjectWithTag("Map");
            Destroy(previousMap);

            currentMap = Instantiate(mapPrefabs[mapOrder[currentMapIndex]]);

            // dem server sagen, dass wir ready für die nächste runde sind
            using (var msg = Message.CreateEmpty((ushort)MessageTag.StartRoundRequest)) {
                ConnectionManager.Instance.Client.SendMessage(msg, SendMode.Reliable);
            }
        }
    }

    private void FixedUpdate() {
        //Debug.Log($"GameManager:FixedUpdate {ClientTick}");
        ClientTick++;
        var dataToProcess = buffer.Get();
        //Debug.Log($"processing {dataToProcess.Length} updates");
        foreach (var data in dataToProcess) {
            UpdateClientGameState(data);
        }

        while (bulletResponses.Count > 0) {
            var bullet = bulletResponses.Dequeue();
            SpawnBullet(bullet);
        }
    }

    // in Parameter werden als Referenz übergeben und sind schreibgeschützt
    private void UpdateClientGameState(in GameUpdateData updateData) {
        LastServerTick = updateData.Frame;
        
        // despawn
        foreach (var despawnData in updateData.DespawnData) {
            DespawnPlayer(despawnData.Id);
        }

        foreach (var bulletDespawnData in updateData.BulletDespawns) {
            DespawnBullet(bulletDespawnData.Id);
        }
        
        // update
        foreach (var playerState in updateData.PlayerStates) {
            if (activePlayers.TryGetValue(playerState.Id, out ClientPlayer p)) {
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
        player.BulletHit += OnPlayerBulletHit;
        player.Initialize(spawnData.Id, spawnData.Name, spawnData.Position);
        activePlayers.Add(spawnData.Id, player);
    }

    private void DespawnPlayer(ushort id) {
        if (activePlayers.TryGetValue(id, out ClientPlayer p)) {
            p.BulletHit -= OnPlayerBulletHit;
            Destroy(p.gameObject);
            activePlayers.Remove(id);
        } else if (inactivePlayers.TryGetValue(id, out p)) {
            p.BulletHit -= OnPlayerBulletHit;
            Destroy(p.gameObject);
            inactivePlayers.Remove(id);
        }
    }

    // Wenn ein Spieler getroffen wird deaktivieren wir ihn nur.
    // Zerstört werden Spieler erst wenn sie das Spiel beenden oder disconnecten.
    private void OnPlayerBulletHit(ushort playerId) { 
        if (activePlayers.TryGetValue(playerId, out ClientPlayer p)) {
            activePlayers.Remove(playerId);

            p.gameObject.SetActive(false);
            inactivePlayers.Add(playerId, p);
        }

        if (activePlayers.Count < 2) {
            Debug.Log("round over");
        }
    }

    private void OnBulletResponse(BulletResponseData responseData) => bulletResponses.Enqueue(responseData);

    private void SpawnBullet(BulletResponseData responseData) {
        if (activePlayers.TryGetValue(responseData.PlayerId, out ClientPlayer p)) {
            var go = bulletPool.Obtain(true);
            var bullet = go.GetComponent<ClientBullet>();
            var spawnData = new BulletSpawnData(
                responseData.BulletId,
                responseData.PlayerId,
                p.GunPoint.position,
                p.transform.forward
            );
            bullet.Initialize(spawnData);
            activeBullets[spawnData.Id] = bullet;
        }
    }

    private void DespawnBullet(ushort bulletId) {
        if (activeBullets.TryGetValue(bulletId, out ClientBullet bullet)) {
            //Debug.Log($"despawning bullet {bulletId}");
            bulletPool.Free(bullet.gameObject);
            activeBullets.Remove(bulletId);
        }
    }

}

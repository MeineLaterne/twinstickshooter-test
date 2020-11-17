﻿using DarkRift;
using DarkRift.Client;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    
    public static GameManager Instance { get; private set; }
    
    public uint ClientTick { get; private set; }
    public uint LastServerTick { get; private set; }

    [SerializeField] private GameObject[] playerPrefabs;

    private readonly Dictionary<ushort, ClientPlayer> players = new Dictionary<ushort, ClientPlayer>();
    private readonly QueueBuffer<GameUpdateData> buffer = new QueueBuffer<GameUpdateData>(1);

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
                case MessageTag.StartGameResponse:
                    OnGameStart(msg.Deserialize<GameStartData>());
                    break;
                case MessageTag.GameUpdate:
                    OnGameUpdate(msg.Deserialize<GameUpdateData>());
                    break;
            }
        }
    }

    private void OnGameStart(GameStartData startData) { }

    private void OnGameUpdate(GameUpdateData updateData) => buffer.Add(updateData);

    private void FixedUpdate() {
        ClientTick++;
        var dataToProcess = buffer.Get();
        foreach (var data in dataToProcess) {
            UpdateClientGameState(data);
        }
    }

    private void UpdateClientGameState(GameUpdateData updateData) {
        LastServerTick = updateData.Frame;

        foreach (var spawnData in updateData.SpawnData) {
            if (spawnData.Id != ConnectionManager.Instance.PlayerID) {
                SpawnPlayer(spawnData);
            }
        }

        foreach (var despawnData in updateData.DespawnData) {
            if (players.ContainsKey(despawnData.Id)) {
                Destroy(players[despawnData.Id].gameObject);
                players.Remove(despawnData.Id);
            }
        }

        foreach (var playerState in updateData.PlayerStates) {
            if (players.TryGetValue(playerState.Id, out ClientPlayer p)) {
                p.UpdatePlayerState(playerState);
            }
        }

    }

    private void SpawnPlayer(PlayerSpawnData spawnData) {
        var go = Instantiate(playerPrefabs[spawnData.PrefabIndex]);
        var player = go.GetComponent<ClientPlayer>();
        player.Initialize(spawnData.Id, spawnData.Name);
        players.Add(spawnData.Id, player);
    }

}

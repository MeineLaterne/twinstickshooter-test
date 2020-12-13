using DarkRift;
using DarkRift.Server;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ServerPlayer : MonoBehaviour {

    public PlayerController PlayerController { get; private set; }
    public uint InputTick { get; private set; }
    public PlayerStateData PlayerState { get; private set; }
    public GunPointData GunPointState => new GunPointData {
        Position = gunPoint.position,
        Direction = transform.forward
    };

    public List<PlayerStateData> History { get; } = new List<PlayerStateData>();
    public List<GunPointData> GunPointHistory { get; } = new List<GunPointData>();
    public IClient Client => clientConnection.client;
    public Room Room => clientConnection.Room;
    
    [SerializeField] private Transform gunPoint;

    private ClientConnection clientConnection;

    private readonly Dictionary<ushort, ServerBullet> bullets = new Dictionary<ushort, ServerBullet>();

    private readonly QueueBuffer<PlayerInputData> inputBuffer = new QueueBuffer<PlayerInputData>(1);
    private PlayerInputData[] inputsToProcess;

    public struct GunPointData {
        public Vector3 Position;
        public Vector3 Direction;
    }

    internal void Initialize(Vector3 position, ClientConnection clientConnection) {
        this.clientConnection = clientConnection;
        this.clientConnection.ServerPlayer = this;

        InputTick = 0;
        
        PlayerState = new PlayerStateData(clientConnection.client.ID, 0, position, Quaternion.identity);

        transform.position = position;
    }

    internal void Teleport(Vector3 position) {
        PlayerState = new PlayerStateData(clientConnection.client.ID, 0, position, Quaternion.identity);
        transform.position = position;
    }

    internal void ReceiveInput(PlayerInputData inputData) => inputBuffer.Add(inputData);

    internal void ReceiveBulletInput(BulletInputData inputData) {
        if (bullets.TryGetValue(inputData.Id, out ServerBullet bullet)) {
            bullet.ReceiveInput(inputData);
        }
    }

    internal void PlayerPreUpdate() {
        inputsToProcess = inputBuffer.Get();
    }

    internal PlayerStateData PlayerUpdate() {
        
        var shoot = false;
        foreach (var inputData in inputsToProcess) {
            InputTick = inputData.InputTick;
            shoot = shoot || inputData.Inputs[0];
            PlayerState = PlayerController.GetNextFrameData(inputData, PlayerState);
        }

        transform.localPosition = PlayerState.Position;
        transform.localRotation = PlayerState.Rotation;

        return PlayerState;
    }

    // TODO: prefab index richtig an andere clients weitergeben
    public PlayerSpawnData GetSpawnData() {
        return new PlayerSpawnData(clientConnection.client.ID, 0, clientConnection.userName, transform.localPosition);
    }

    internal void AddBullet(ServerBullet bullet) {
        bullets[bullet.Id] = bullet;
    }

    internal void RemoveBullet(ushort bulletId) {
        bullets.Remove(bulletId);
    }

    internal void OnBulletHit() {
        Room.DespawnPlayer(this);
    }

    private void Awake() {
        PlayerController = GetComponent<PlayerController>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        transform.localPosition = PlayerState.Position;
    }

    private void OnDestroy() {
        if (clientConnection != null) {
            clientConnection.ServerPlayer = null;
        }
    }
}

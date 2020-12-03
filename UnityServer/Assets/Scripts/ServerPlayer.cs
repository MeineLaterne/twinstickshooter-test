using DarkRift;
using DarkRift.Server;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ServerPlayer : MonoBehaviour {

    public PlayerController PlayerController { get; private set; }
    public uint InputTick { get; private set; }
    public PlayerStateData PlayerState { get; private set; }
    public GunPointData GunPointState { get; private set; }
    public List<PlayerStateData> History { get; } = new List<PlayerStateData>();
    public List<GunPointData> GunPointHistory { get; } = new List<GunPointData>();
    public IClient Client => clientConnection.client;
    public Room Room => clientConnection.Room;
    
    [SerializeField] private Transform gunPoint;

    private ClientConnection clientConnection;
    private readonly QueueBuffer<PlayerInputData> inputBuffer = new QueueBuffer<PlayerInputData>(1);
    private PlayerInputData[] inputsToProcess;

    private bool shotLock;

    public struct GunPointData {
        public Vector3 Position;
        public Vector3 Direction;
    }

    public void Initialize(Vector3 position, ClientConnection clientConnection) {
        this.clientConnection = clientConnection;
        this.clientConnection.ServerPlayer = this;

        InputTick = 0;
        
        PlayerState = new PlayerStateData(clientConnection.client.ID, 0, position, Quaternion.identity);
        
        

    }

    public void ReceiveInput(PlayerInputData inputData) => inputBuffer.Add(inputData);

    public void PlayerPreUpdate() {
        inputsToProcess = inputBuffer.Get();
    }

    public PlayerStateData PlayerUpdate() {
        
        var shoot = false;
        foreach (var inputData in inputsToProcess) {
            InputTick = inputData.InputTick;
            shoot = shoot || inputData.Inputs[0];
            PlayerState = PlayerController.GetNextFrameData(inputData, PlayerState);
        }

        transform.localPosition = PlayerState.Position;
        transform.localRotation = PlayerState.Rotation;

        GunPointState = new GunPointData {
            Position = gunPoint.position,
            Direction = transform.forward
        };

        return PlayerState;
    }

    // TODO: prefab index richtig an andere clients weitergeben
    public PlayerSpawnData GetSpawnData() {
        return new PlayerSpawnData(clientConnection.client.ID, 0, clientConnection.userName, transform.localPosition);
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

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
    private readonly QueueBuffer<PlayerInputData> inputBuffer = new QueueBuffer<PlayerInputData>(1, 2);
    private PlayerInputData[] inputsToProcess;

    private bool shotLock;

    public struct GunPointData {
        public Vector3 Position;
        public Vector3 Direction;
    }

    public void Initialize(Vector3 position, ClientConnection clientConnection) {
        this.clientConnection = clientConnection;
        this.clientConnection.ServerPlayer = this;

        PlayerState = new PlayerStateData(clientConnection.client.ID, position, Quaternion.identity);
        InputTick = clientConnection.Room.ServerTick;

    }

    public void ReceiveInput(PlayerInputData inputData) => inputBuffer.Add(inputData);

    public void PlayerPreUpdate() {
        inputsToProcess = inputBuffer.Get();

        foreach (var inputData in inputsToProcess) {
            if (inputData.Inputs[0]) {
                if (!shotLock) {
                    shotLock = true;
                    Room.SpawnBullet(inputData.Time, this);
                }
            } else {
                shotLock = false;
            }
        }
    }

    public PlayerStateData PlayerUpdate() {
        if (inputsToProcess.Length != 0) {
            var inputData = inputsToProcess[0];
            InputTick++;

            for (int i = 1; i < inputsToProcess.Length; i++) {
                InputTick++;
                for (int j = 0; j < inputData.Inputs.Length; j++) {
                    inputData.Inputs[j] = inputData.Inputs[j] || inputsToProcess[i].Inputs[j];
                }
                inputData.MovementAxes = inputsToProcess[i].MovementAxes;
                inputData.RotationAxes = inputsToProcess[i].RotationAxes;
            }

            PlayerState = PlayerController.GetNextFrameData(inputData, PlayerState);
            GunPointState = new GunPointData {
                Position = gunPoint.position,
                Direction = transform.forward
            };
        }

        transform.localPosition = PlayerState.Position;
        transform.localRotation = PlayerState.Rotation;

        History.Add(PlayerState);
        
        if (History.Count > 10) {
            History.RemoveAt(0);
        }

        GunPointHistory.Add(GunPointState);

        if (GunPointHistory.Count > 10) {
            GunPointHistory.RemoveAt(0);
        }

        return PlayerState;
    }

    // TODO: prefab index richtig an andere clients weitergeben
    public PlayerSpawnData GetSpawnData() {
        return new PlayerSpawnData(clientConnection.client.ID, 0, clientConnection.userName, transform.localPosition);
    }

    private void Awake() {
        PlayerController = GetComponent<PlayerController>();
    }

    private void OnDestroy() {
        if (clientConnection != null) {
            clientConnection.ServerPlayer = null;
        }
    }
}

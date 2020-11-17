using DarkRift;
using DarkRift.Server;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ServerPlayer : MonoBehaviour {

    public PlayerController PlayerController { get; private set; }
    public uint InputTick { get; private set; }
    public PlayerStateData PlayerState { get; private set; }
    public IClient Client => clientConnection.client;
    public Room Room => clientConnection.Room;


    private ClientConnection clientConnection;
    private QueueBuffer<PlayerInputData> inputBuffer = new QueueBuffer<PlayerInputData>(1, 2);
    private PlayerInputData[] inputsToProcess;

    public void Initialize(Vector3 position, ClientConnection clientConnection) {
        this.clientConnection = clientConnection;
        this.clientConnection.ServerPlayer = this;

        PlayerState = new PlayerStateData(clientConnection.client.ID, position, Quaternion.identity);
        InputTick = clientConnection.Room.ServerTick;

        var spawnData = clientConnection.Room.GetAllSpawnData();

        using (var msg = Message.Create((ushort)MessageTag.StartGameResponse, new GameStartData(InputTick, spawnData))) {
            clientConnection.client.SendMessage(msg, SendMode.Reliable);
        }
    }

    public void ReceiveInput(PlayerInputData inputData) => inputBuffer.Add(inputData);

    public void PlayerPreUpdate() {
        inputsToProcess = inputBuffer.Get();
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
        }

        transform.localPosition = PlayerState.Position;
        transform.localRotation = PlayerState.Rotation;

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

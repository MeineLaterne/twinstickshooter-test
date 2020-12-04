using DarkRift;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BulletController))]
[RequireComponent(typeof(IInputReader<BulletInputData>))]
public class ClientBullet : MonoBehaviour {

    internal ushort Id { get; private set; }
    internal Vector3 Direction { get; private set; }

    private bool isLocal;
    private ushort playerId;
    private BulletController bulletController;
    private StateInterpolation<BulletStateData> interpolation;
    private IInputReader<BulletInputData> inputReader;

    private readonly Queue<BulletReconciliationInfo> history = new Queue<BulletReconciliationInfo>();

    internal void Initialize(BulletSpawnData spawnData) {
        Id = spawnData.Id;
        playerId = spawnData.PlayerId;

        isLocal = playerId == ConnectionManager.Instance.Client.ID;

        transform.position = spawnData.Position;
        Direction = spawnData.Velocity;

        interpolation.CurrentStateData = new BulletStateData(Id, playerId, 0, spawnData.Position);

        GetComponent<CharacterController>().enabled = true;
    }

    internal void UpdateBulletState(BulletStateData stateData) {
        if (!isLocal) {
            interpolation.PushStateData(stateData);
            return;
        }

        if (history.Count == 0)
            return;

        while (history.Count > 0 && history.Peek().InputTick < stateData.InputTick) {
            history.Dequeue();
        }

        if (history.Peek().InputTick != stateData.InputTick)
            return;

        var predictedState = history.Dequeue();

        if (Vector3.Distance(predictedState.StateData.Position, stateData.Position) < 0.05f)
            return;

        interpolation.CurrentStateData = stateData;
        
        var h = history.ToArray();
        foreach (var ri in h) {
            bulletController.ResetTo(interpolation.CurrentStateData);
            var sd = bulletController.GetNextFrameData(ri.InputData, interpolation.CurrentStateData);
            interpolation.PushStateData(sd);
        }

    }

    private void OnControllerVelocityChanged(Vector3 velocity) {
        Direction = Vector3.Normalize(velocity);
    }

    private void Awake() {
        bulletController = GetComponent<BulletController>();
        inputReader = GetComponent<IInputReader<BulletInputData>>();
        interpolation = new StateInterpolation<BulletStateData>(new BulletStateInterpolator(transform));

        bulletController.VelocityChanged += OnControllerVelocityChanged;
    }

    private void FixedUpdate() {
        if (!isLocal) return;

        var inputData = inputReader.ReadInput();

        bulletController.ResetTo(interpolation.CurrentStateData);

        var nextStateData = bulletController.GetNextFrameData(inputData, interpolation.CurrentStateData);

        interpolation.PushStateData(nextStateData);

        using (var msg = Message.Create((ushort)MessageTag.BulletInput, inputData)) {
            ConnectionManager.Instance.Client.SendMessage(msg, SendMode.Reliable);
        }

        history.Enqueue(new BulletReconciliationInfo(nextStateData.InputTick, nextStateData, inputData));
    }

    private void Update() {
        interpolation.Interpolate();
    }

    private void OnDisable() {
        GetComponent<CharacterController>().enabled = false;
    }

    private void OnDestroy() {
        if (bulletController != null) {
            bulletController.VelocityChanged -= OnControllerVelocityChanged;
        }
    }

}

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BulletController))]
public class ClientBullet : MonoBehaviour {

    public ushort Id { get; private set; }

    private bool isLocal;
    private ushort playerId;
    private BulletController bulletController;
    private StateInterpolation<BulletStateData> interpolation;

    private readonly Queue<BulletReconciliationInfo> history = new Queue<BulletReconciliationInfo>();

    public void Initialize(BulletSpawnData spawnData) {
        Id = spawnData.Id;
        playerId = spawnData.PlayerId;

        isLocal = playerId == ConnectionManager.Instance.Client.ID;

        transform.position = spawnData.Position;
        bulletController.Velocity = spawnData.Velocity;
        interpolation.CurrentStateData = new BulletStateData(Id, playerId, 0, spawnData.Position);

        GetComponent<CharacterController>().enabled = true;
    }

    public void UpdateBulletState(BulletStateData stateData) {
        if (!isLocal) {
            interpolation.PushStateData(stateData);
            return;
        }

        if (history.Count == 0)
            return;

        while (history.Count > 0 && history.Peek().Frame < stateData.Frame) {
            history.Dequeue();
        }

        var predictedState = history.Dequeue();

        if (Vector3.Distance(predictedState.StateData.Position, stateData.Position) < 0.05f)
            return;

        interpolation.CurrentStateData = stateData;
        transform.position = stateData.Position;

        var reconSteps = history.Count;
        for (var i = 0; i < reconSteps; i++) {
            var sd = bulletController.GetNextFrameData(interpolation.CurrentStateData);
            interpolation.PushStateData(sd);
        }
        
    }

    private void Awake() {
        bulletController = GetComponent<BulletController>();
        interpolation = new StateInterpolation<BulletStateData>(new BulletStateInterpolator(transform));
    }

    private void FixedUpdate() {
        if (!isLocal) return;

        transform.position = interpolation.CurrentStateData.Position;

        var nextStateData = bulletController.GetNextFrameData(interpolation.CurrentStateData);

        interpolation.PushStateData(nextStateData);

        // nur zum testen
        transform.position = interpolation.CurrentStateData.Position;

        history.Enqueue(new BulletReconciliationInfo(nextStateData.Frame, nextStateData));
    }

    private void Update() {
        //interpolation.Interpolate();
    }

    private void OnDisable() {
        GetComponent<CharacterController>().enabled = false;
    }

}

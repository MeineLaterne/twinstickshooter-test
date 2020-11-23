using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BulletController))]
public class ClientBullet : MonoBehaviour {

    private BulletController bulletController;
    private StateInterpolation<BulletStateData> interpolation;
    
    private ushort id;
    private ushort playerId;
    private bool isLocal;

    private bool logLock;

    private readonly Queue<BulletReconciliationInfo> history = new Queue<BulletReconciliationInfo>();

    public void Initialize(BulletSpawnData spawnData) {
        id = spawnData.Id;
        playerId = spawnData.PlayerId;

        transform.position = spawnData.Position;
        bulletController.Velocity = spawnData.Velocity;

        if (playerId == ConnectionManager.Instance.PlayerId) {
            isLocal = true;
            interpolation.CurrentStateData = new BulletStateData(id, playerId, spawnData.Position);
        }
    }

    public void UpdateBulletState(BulletStateData stateData) {
        if (isLocal) {

            while (history.Count != 0 && history.Peek().Frame < GameManager.Instance.LastServerTick) {
                history.Dequeue();
            }

            if (history.Count != 0 && history.Peek().Frame == GameManager.Instance.LastServerTick) {
                var ri = history.Dequeue();
                if (Vector3.Distance(ri.StateData.Position, stateData.Position) > 0.05f) {
                    
                    if (!logLock) {
                        logLock = true;
                        Debug.Log($"ClientBullet reconciliation");
                        Debug.Log($"ClientTick: {GameManager.Instance.ClientTick} ServerTick: {GameManager.Instance.LastServerTick}");
                        Debug.Log($"history to apply: {history.Count}");
                    }
                    
                    interpolation.CurrentStateData = stateData;
                    transform.position = stateData.Position;

                    var infos = history.ToArray();
                    for (var i = 0; i < infos.Length; i++) {
                        var sd = bulletController.GetNextFrameData(interpolation.CurrentStateData);
                        interpolation.PushStateData(sd);
                    }
                } else {
                    logLock = false;
                }
            }

        } else {
            interpolation.PushStateData(stateData);
        }
    }

    private void Awake() {
        bulletController = GetComponent<BulletController>();
        interpolation = new StateInterpolation<BulletStateData>(Interpolate);
    }

    private void Update() {
        interpolation.Interpolate();
    }

    private void FixedUpdate() {
        if (!isLocal) return;

        transform.position = interpolation.CurrentStateData.Position;

        var nextState = bulletController.GetNextFrameData(interpolation.CurrentStateData);

        interpolation.PushStateData(nextState);

        history.Enqueue(new BulletReconciliationInfo(GameManager.Instance.ClientTick, nextState));
    }

    private void OnDisable() {
        bulletController.Velocity = Vector3.zero;
    }

    private void Interpolate(BulletStateData previous, BulletStateData current, float t) {
        transform.position = Vector3.LerpUnclamped(previous.Position, current.Position, t);
    }

}

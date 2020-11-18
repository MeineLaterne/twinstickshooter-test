using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BulletController))]
public class ClientBullet : MonoBehaviour {

    private BulletController bulletController;
    private StateInterpolation<BulletStateData> interpolation;
    
    private ushort id;
    private ushort playerId;
    private bool isLocal;

    private readonly Queue<BulletReconciliationInfo> history = new Queue<BulletReconciliationInfo>();

    public void Initialize(BulletSpawnData spawnData) {
        id = spawnData.Id;
        playerId = spawnData.PlayerId;
        
        bulletController.Velocity = spawnData.Velocity;

        if (playerId == ConnectionManager.Instance.PlayerID) {
            isLocal = true;
            interpolation.CurrentStateData = new BulletStateData(id, playerId, spawnData.Position);
        }
    }

    public void UpdateBulletState(BulletStateData stateData) {
        if (isLocal) {
            // .. reconciliation
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
    }

    private void Interpolate(BulletStateData previous, BulletStateData current, float t) {
        transform.position = Vector3.LerpUnclamped(previous.Position, current.Position, t);
    }

}

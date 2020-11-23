using System.Collections.Generic;
using UnityEngine;

public class ClientBullet : MonoBehaviour {

    public ushort Id => id;

    //private BulletController bulletController;
    private StateInterpolation<BulletStateData> interpolation;
    
    private ushort id;
    private ushort playerId;

    private int cntr;

    public void Initialize(BulletSpawnData spawnData) {
        id = spawnData.Id;
        playerId = spawnData.PlayerId;

        transform.position = spawnData.Position;

        //bulletController.Velocity = spawnData.Velocity;

        //interpolation.CurrentStateData = new BulletStateData(id, playerId, spawnData.Position);

        //if (playerId == ConnectionManager.Instance.PlayerId) {
        //    isLocal = true;
        //    interpolation.CurrentStateData = new BulletStateData(id, playerId, spawnData.Position);
        //}
    }

    public void UpdateBulletState(BulletStateData stateData) {
        //interpolation.PushStateData(stateData);
        transform.position = stateData.Position;

        cntr++;
        if (cntr % 20 == 0) {
            cntr = 0;
            Debug.Log($"bullet {id} received: {stateData.Position} actual: {transform.position}");
        }
    }

    private void Awake() {
        //bulletController = GetComponent<BulletController>();
        //interpolation = new StateInterpolation<BulletStateData>(Interpolate);
    }

    //private void Update() {
    //    interpolation.Interpolate();
    //}

    //private void FixedUpdate() {
    //    if (!isLocal) return;

    //    transform.position = interpolation.CurrentStateData.Position;

    //    var nextState = bulletController.GetNextFrameData(interpolation.CurrentStateData);

    //    interpolation.PushStateData(nextState);

    //    history.Enqueue(new BulletReconciliationInfo(GameManager.Instance.ClientTick, nextState));
    //}

    private void OnDisable() {
        //bulletController.Velocity = Vector3.zero;
    }

    private void Interpolate(BulletStateData previous, BulletStateData current, float t) {
        transform.position = Vector3.LerpUnclamped(previous.Position, current.Position, t);
    }

}

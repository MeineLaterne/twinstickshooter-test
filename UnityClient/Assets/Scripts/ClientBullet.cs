using System.Collections.Generic;
using UnityEngine;

public class ClientBullet : MonoBehaviour {

    public ushort Id => id;

    //private BulletController bulletController;
    private StateInterpolation<BulletStateData> interpolation;
    
    private ushort id;
    private ushort playerId;

    public void Initialize(BulletSpawnData spawnData) {
        id = spawnData.Id;
        playerId = spawnData.PlayerId;

        transform.position = spawnData.Position;
        interpolation.CurrentStateData = new BulletStateData(id, playerId, spawnData.Position);
        
    }

    public void UpdateBulletState(BulletStateData stateData) {
        interpolation.PushStateData(stateData);
        transform.position = stateData.Position;
    }

    private void Awake() {
        interpolation = new StateInterpolation<BulletStateData>(new BulletStateInterpolator(transform));
    }

    private void Update() {
        //interpolation.Interpolate();
    }

}

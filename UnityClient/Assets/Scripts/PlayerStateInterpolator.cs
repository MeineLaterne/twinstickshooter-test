using UnityEngine;

public class PlayerStateInterpolator : IInterpolator<PlayerStateData> {

    private Transform transform;

    public PlayerStateInterpolator(Transform transform) {
        this.transform = transform;
    }

    ~PlayerStateInterpolator() {
        transform = null;
    }
    
    public void Interpolate(PlayerStateData previousState, PlayerStateData currentState, float t) {
        transform.position = Vector3.LerpUnclamped(previousState.Position, currentState.Position, t);
        transform.rotation = Quaternion.SlerpUnclamped(previousState.Rotation, currentState.Rotation, t);
    }
}

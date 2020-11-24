using UnityEngine;

public class PlayerStateInterpolator : IInterpolator<PlayerStateData> {
    public void Interpolate(Transform transform, PlayerStateData previousState, PlayerStateData currentState, float t) {
        transform.position = Vector3.LerpUnclamped(previousState.Position, currentState.Position, t);
        transform.rotation = Quaternion.LerpUnclamped(previousState.Rotation, currentState.Rotation, t);
    }
}

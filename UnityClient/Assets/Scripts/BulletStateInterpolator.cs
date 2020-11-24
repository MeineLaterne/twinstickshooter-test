using UnityEngine;
public class BulletStateInterpolator : IInterpolator<BulletStateData> {
    public void Interpolate(Transform transform, BulletStateData previousState, BulletStateData currentState, float t) {
        transform.position = Vector3.LerpUnclamped(previousState.Position, currentState.Position, t);
    }
}
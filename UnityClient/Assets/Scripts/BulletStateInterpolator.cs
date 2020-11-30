using UnityEngine;
public class BulletStateInterpolator : IInterpolator<BulletStateData> {
    private Transform transform;

    public BulletStateInterpolator(Transform transform) {
        this.transform = transform;
    }

    ~BulletStateInterpolator() {
        transform = null;
    }

    public void Interpolate(BulletStateData previousState, BulletStateData currentState, float t) {
        transform.position = Vector3.LerpUnclamped(previousState.Position, currentState.Position, t);
    }
}
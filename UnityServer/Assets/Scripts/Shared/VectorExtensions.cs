using UnityEngine;
public static class VectorExtensions {
    public static Vector2 Perpendicular(this Vector2 v) {
        return new Vector2(-v.y, v.x);
    }

    public static Vector3 Bounce(this Vector3 d, Vector3 normal) {
        return d - 2 * Vector3.Dot(d, normal) * normal;
    }
}

using UnityEngine;
public static class VectorExtensions {
    public static Vector2 Perpendicular(this Vector2 v) => new Vector2(-v.y, v.x);

    public static Vector3 Bounce(this Vector3 v, Vector3 normal) => v - 2 * Vector3.Dot(v, normal) * normal;

    public static Vector3 Slide(this Vector3 v, Vector3 normal) => v - normal * Vector3.Dot(v, normal);

}

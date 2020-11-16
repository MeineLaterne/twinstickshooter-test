using UnityEngine;
public static class Vector2Extensions {
    public static Vector2 Perpendicular(this Vector2 v) {
        return new Vector2(-v.y, v.x);
    }
}

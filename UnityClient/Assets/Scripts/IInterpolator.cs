
public interface IInterpolator<T> {
    void Interpolate(UnityEngine.Transform transform, T previousState, T currentState, float t);
}
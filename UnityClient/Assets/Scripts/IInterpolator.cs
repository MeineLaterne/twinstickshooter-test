
public interface IInterpolator<T> {
    void Interpolate(T previousState, T currentState, float t);
}
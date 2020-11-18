public interface IInterpolatable {
    void Interpolate(UnityEngine.Transform transform, IInterpolatable nextState, float t);
}
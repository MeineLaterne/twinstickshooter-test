
using UnityEngine;

public class StateInterpolation<T> {

    public T CurrentStateData { get; set; }
    public T PreviousStateData { get; private set; }

    public delegate void Interpolator(T previousState, T currentState, float t);

    private Interpolator interpolator;
    private float lastInputTime;

    public StateInterpolation(Interpolator interpolator) {
        this.interpolator = interpolator;
    }

    ~StateInterpolation() {
        interpolator = null;
    }

    public void Interpolate() {
        var delta = Time.time - lastInputTime;
        var t = delta / Time.fixedDeltaTime;

        interpolator?.Invoke(PreviousStateData, CurrentStateData, t);
    }

    public void PushStateData(T data) => Refresh(data, CurrentStateData);

    private void Refresh(T currentStateData, T previousStateData) {
        CurrentStateData = currentStateData;
        PreviousStateData = previousStateData;
        lastInputTime = Time.fixedTime;
    }

}
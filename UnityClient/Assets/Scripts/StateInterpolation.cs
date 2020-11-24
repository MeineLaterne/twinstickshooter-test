
using UnityEngine;

public class StateInterpolation<T> {

    public T CurrentStateData { get; set; }
    public T PreviousStateData { get; private set; }

    private IInterpolator<T> interpolator;
    private float lastFixedTime;

    public StateInterpolation(IInterpolator<T> interpolator) {
        this.interpolator = interpolator;
    }

    ~StateInterpolation() {
        interpolator = null;
    }

    public void Interpolate(Transform transform) {
        var delta = Time.time - lastFixedTime;
        var t = delta / Time.fixedDeltaTime;

        interpolator.Interpolate(transform, PreviousStateData, CurrentStateData, t);
    }

    public void PushStateData(T data) => Refresh(data, CurrentStateData);

    private void Refresh(T currentStateData, T previousStateData) {
        CurrentStateData = currentStateData;
        PreviousStateData = previousStateData;
        lastFixedTime = Time.fixedTime;
    }

}
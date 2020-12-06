
using UnityEngine;

internal class StateInterpolation<T> {

    internal T CurrentStateData { get; set; }
    internal T PreviousStateData { get; private set; }

    private readonly IInterpolator<T> interpolator;
    private float lastFixedTime;

    internal StateInterpolation(IInterpolator<T> interpolator) {
        this.interpolator = interpolator;
    }

    internal void Initialize(T initialState) {
        CurrentStateData = initialState;
        PreviousStateData = initialState;
    }

    internal void Interpolate() {
        var delta = Time.time - lastFixedTime;
        var t = delta / Time.fixedDeltaTime;

        interpolator.Interpolate(PreviousStateData, CurrentStateData, t);
    }

    internal void PushStateData(T data) => Refresh(data, CurrentStateData);

    private void Refresh(T currentStateData, T previousStateData) {
        CurrentStateData = currentStateData;
        PreviousStateData = previousStateData;
        lastFixedTime = Time.fixedTime;
    }

}
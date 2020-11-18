using UnityEngine;

public class BulletInterpolation : MonoBehaviour {
    public BulletStateData CurrentStateData { get; set; }
    public BulletStateData PreviousStateData { get; private set; }

    private float lastInputTime;

    public void PushStateData(BulletStateData data) => Refresh(data, CurrentStateData);

    private void Refresh(BulletStateData currentStateData, BulletStateData previousStateData) {
        CurrentStateData = currentStateData;
        PreviousStateData = previousStateData;
        lastInputTime = Time.fixedTime;
    }

    private void Update() {
        var delta = Time.time - lastInputTime;
        var t = delta / Time.fixedDeltaTime;

        transform.position = Vector3.LerpUnclamped(PreviousStateData.Position, CurrentStateData.Position, t);
    }
}
using UnityEngine;

public class PlayerInterpolation : MonoBehaviour {
    
    public PlayerStateData CurrentStateData { get; set; }
    public PlayerStateData PreviousStateData { get; private set; }

    private float lastInputTime;
    
    public void PushStateData(PlayerStateData data) => Refresh(data, CurrentStateData);

    public void Refresh(PlayerStateData currentStateData, PlayerStateData previousStateData) {
        CurrentStateData = currentStateData;
        PreviousStateData = previousStateData;
        lastInputTime = Time.fixedTime;
    }

    private void Update() {
        var delta = Time.time - lastInputTime;
        var t = delta / Time.fixedDeltaTime;

        transform.position = Vector3.LerpUnclamped(PreviousStateData.Position, CurrentStateData.Position, t);
        transform.rotation = Quaternion.SlerpUnclamped(PreviousStateData.Rotation, CurrentStateData.Rotation, t);
    }
}

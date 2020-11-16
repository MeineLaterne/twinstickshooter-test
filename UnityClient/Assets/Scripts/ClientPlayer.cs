using DarkRift;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInterpolation))]
[RequireComponent(typeof(IInputReader))]
public class ClientPlayer : MonoBehaviour {

    private PlayerController playerController;
    private PlayerInterpolation interpolation;
    private IInputReader inputReader;

    private ushort id;
    private bool isLocalPlayer;
    private string playerName;

    public void Initialize(ushort id, string playerName) {
        this.id = id;
        this.playerName = playerName;

        if (this.id == ConnectionManager.Instance.PlayerID) {
            isLocalPlayer = true;
            interpolation.CurrentStateData = new PlayerStateData(this.id, Vector3.zero, Quaternion.identity);
        }
    }

    public void UpdatePlayerState(PlayerStateData playerState) {
        if (isLocalPlayer) {

        } else {
            interpolation.PushStateData(playerState);
        }
    }

    private void Awake() {
        playerController = GetComponent<PlayerController>();
        interpolation = GetComponent<PlayerInterpolation>();
        inputReader = GetComponent<IInputReader>();
    }

    private void FixedUpdate() {

        if (isLocalPlayer) {
            var inputData = inputReader.ReadInput(0);

            // erst wird der Spieler auf die letzte berechnete Position zurückgesetzt
            transform.position = interpolation.CurrentStateData.Position;

            // dann holen wir uns die Daten für den nächsten Frame
            var nextStateData = playerController.GetNextFrameData(inputData, interpolation.CurrentStateData);

            // dann starten wir die Interpolation
            interpolation.PushStateData(nextStateData);

            using (var msg = Message.Create((ushort)MessageTag.GameInput, inputData)) {
                ConnectionManager.Instance.Client.SendMessage(msg, SendMode.Reliable);
            }
        }
    }
}

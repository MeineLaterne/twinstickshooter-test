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

    private void Awake() {
        playerController = GetComponent<PlayerController>();
        interpolation = GetComponent<PlayerInterpolation>();
        inputReader = GetComponent<IInputReader>();
    }

    private void Start() {
        interpolation.CurrentStateData = new PlayerStateData(id, Vector3.zero, Quaternion.identity);
    }

    private void FixedUpdate() {

        var inputData = inputReader.ReadInput(0);

        // erst wird der Spieler auf die letzte berechnete Position zurückgesetzt
        transform.position = interpolation.CurrentStateData.Position;
        
        // dann holen wir uns die Daten für den nächsten Frame
        var nextStateData = playerController.GetNextFrameData(inputData, interpolation.CurrentStateData);

        // dann starten wir die Interpolation
        interpolation.PushStateData(nextStateData);
    }
}

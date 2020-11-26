using DarkRift;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(IInputReader))]
public class ClientPlayer : MonoBehaviour {

    private PlayerController playerController;
    private StateInterpolation<PlayerStateData> interpolation;
    private IInputReader inputReader;

    private ushort id;
    private bool isLocalPlayer;
    private bool shotLock;
    private string playerName;

    // speichert unsere vorhergesagten Informationen zu player state und input
    private readonly Queue<ReconciliationInfo> history = new Queue<ReconciliationInfo>();

    public void Initialize(ushort id, string playerName) {
        this.id = id;
        this.playerName = playerName;

        interpolation = new StateInterpolation<PlayerStateData>(new PlayerStateInterpolator());

        if (this.id == ConnectionManager.Instance.PlayerId) {
            isLocalPlayer = true;
            interpolation.CurrentStateData = new PlayerStateData(this.id, Vector3.zero, Quaternion.identity);
        }
    }

    public void UpdatePlayerState(PlayerStateData playerState) {
        if (isLocalPlayer) {

            // erstmal verwerfen wir alle Elemente deren Frame kleiner als der zuletzt erhaltene ServerTick ist aus unserer history
            while (history.Any() && history.Peek().Frame < GameManager.Instance.LastServerTick) {
                history.Dequeue();
            }

            // dann schauen wir ob unsere vorhergesagten Daten sich krass von den Serverdaten unterscheiden
            if (history.Any() && history.Peek().Frame == GameManager.Instance.LastServerTick) {
                var ri = history.Dequeue();
                if (Vector3.Distance(ri.StateData.Position, playerState.Position) > 0.1f) {
                    // wenn ja setzen wir den Spieler auf die Position, die der Server geschickt hat
                    interpolation.CurrentStateData = playerState;
                    transform.position = playerState.Position;
                    transform.rotation = playerState.Rotation;

                    // jetzt haben wir den Spieler an eine Position aus der Vergangenheit gesetzt
                    // deshalb müssen alle inputs, die noch nicht vom Server bearbeitet wurden, angewandt werden
                    var infos = history.Where((element) => element.Frame > GameManager.Instance.LastServerTick);

                    foreach (var info in infos) {
                        var psd = playerController.GetNextFrameData(info.InputData, interpolation.CurrentStateData);
                        interpolation.PushStateData(psd);
                    }

                    //for (int i = 0; i < infos.Count; i++) {
                    //    Debug.Log($"frame > LastServerTick: {infos[i].Frame > GameManager.Instance.LastServerTick}");
                    //    var psd = playerController.GetNextFrameData(infos[i].InputData, interpolation.CurrentStateData);
                    //    interpolation.PushStateData(psd);
                    //}

                    transform.position = interpolation.CurrentStateData.Position;
                    transform.rotation = interpolation.CurrentStateData.Rotation;

                }
            }
        } else {
            interpolation.PushStateData(playerState);
        }
    }

    private void Awake() {
        playerController = GetComponent<PlayerController>();
        inputReader = GetComponent<IInputReader>();
    }

    private void Update() {
        interpolation.Interpolate(transform);
    }

    private void FixedUpdate() {

        if (!isLocalPlayer) return;

        var inputData = inputReader.ReadInput();

        // erst wird der Spieler auf die letzte berechnete Position zurückgesetzt
        transform.position = interpolation.CurrentStateData.Position;

        // dann holen wir uns die Daten für den nächsten Frame
        var nextStateData = playerController.GetNextFrameData(inputData, interpolation.CurrentStateData);

        // dann starten wir die Interpolation
        interpolation.PushStateData(nextStateData);

        if (inputData.Inputs[0]) {
            if (!shotLock) {
                Debug.Log($"sending shot input with time: {inputData.Frame}");
                shotLock = true;
            }
        } else {
            shotLock = false;
        }

        Debug.Log($"sending input {inputData.Frame}");

        // ...senden den input an den Server
        using (var msg = Message.Create((ushort)MessageTag.GameInput, inputData)) {
            ConnectionManager.Instance.Client.SendMessage(msg, SendMode.Reliable);
        }

        // außerdem cachen wir unsere vorhergesagten Informationen
        history.Enqueue(new ReconciliationInfo(inputData.Frame, nextStateData, inputData));
    }

}

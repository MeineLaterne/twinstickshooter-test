﻿using DarkRift;
using System.Collections.Generic;
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

        interpolation = new StateInterpolation<PlayerStateData>(new PlayerStateInterpolator(transform));

        if (this.id == ConnectionManager.Instance.PlayerId) {
            isLocalPlayer = true;
            interpolation.CurrentStateData = new PlayerStateData(this.id, 0, Vector3.zero, Quaternion.identity);
        }
    }

    public void UpdatePlayerState(PlayerStateData playerState) {
        if (!isLocalPlayer) {
            interpolation.PushStateData(playerState);
            return;
        } 
        
        if (history.Count == 0)
            return;

        // erstmal verwerfen wir alle inputs die bereits vom server authorisiert wurden
        while (history.Count > 0 && history.Peek().InputTick < playerState.InputTick) {
            history.Dequeue();
        }

        if (history.Peek().InputTick != playerState.InputTick)
            return;

        var predictedState = history.Dequeue();

        if (Vector3.Distance(predictedState.StateData.Position, playerState.Position) < 0.05f)
            return;

        Debug.Log($"start reconciliation for frame {predictedState.InputTick} : {playerState.InputTick}");
        // dann setzen wir den Spieler auf den letzten authorisierten Zustand
        interpolation.CurrentStateData = playerState;
        transform.position = playerState.Position;
        transform.rotation = playerState.Rotation;

        // dann wenden wir alle noch nicht authorisierten inputs wieder an
        if (history.Count != 0) {
            var reconciliationInfos = history.ToArray();
            foreach (var ri in reconciliationInfos) {
                var psd = playerController.GetNextFrameData(ri.InputData, interpolation.CurrentStateData);
                interpolation.PushStateData(psd);
            }
        }
    }

    private void Awake() {
        playerController = GetComponent<PlayerController>();
        inputReader = GetComponent<IInputReader>();
    }

    private void Update() {
        //interpolation.Interpolate();
    }

    private void FixedUpdate() {
        //Debug.Log("FixedUpdate");
        if (!isLocalPlayer) return;

        var inputData = inputReader.ReadInput();

        // erst wird der Spieler auf die letzte berechnete Position zurückgesetzt
        transform.position = interpolation.CurrentStateData.Position;
        transform.rotation = interpolation.CurrentStateData.Rotation;
        // dann holen wir uns die Daten für den nächsten Frame
        var nextStateData = playerController.GetNextFrameData(inputData, interpolation.CurrentStateData);

        // dann starten wir die Interpolation
        interpolation.PushStateData(nextStateData);

        transform.position = interpolation.CurrentStateData.Position;
        transform.rotation = interpolation.CurrentStateData.Rotation;

        if (inputData.Inputs[0]) {
            if (!shotLock) {
                Debug.Log($"sending shot input with time: {inputData.InputTick}");
                shotLock = true;
            }
        } else {
            shotLock = false;
        }

        //Debug.Log($"sending input {inputData.Frame}");

        // ...senden den input an den Server
        using (var msg = Message.Create((ushort)MessageTag.GameInput, inputData)) {
            ConnectionManager.Instance.Client.SendMessage(msg, SendMode.Reliable);
        }

        // außerdem cachen wir unsere vorhergesagten Informationen
        history.Enqueue(new ReconciliationInfo(inputData.InputTick, nextStateData, inputData));
    }

}

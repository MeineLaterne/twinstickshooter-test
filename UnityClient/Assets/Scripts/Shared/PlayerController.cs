using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    [SerializeField] private float movementSpeed;
    
    public CharacterController CharacterController { get; private set; }

    private void Awake() {
        CharacterController = GetComponent<CharacterController>();
    }

    public PlayerStateData GetNextFrameData(PlayerInputData inputData, PlayerStateData currentStateData) {

        var applyRotation = inputData.Inputs[1];

        var movement = new Vector3(inputData.MovementAxes.x, 0, inputData.MovementAxes.y) * movementSpeed * Time.fixedDeltaTime;
        var lookDirection = new Vector3(inputData.RotationAxes.x, 0, inputData.RotationAxes.y);
        var rotation = applyRotation ? Quaternion.LookRotation(lookDirection, Vector3.up) : transform.rotation;

        CharacterController.enabled = false;
        transform.localPosition = currentStateData.Position;
        transform.localRotation = currentStateData.Rotation;
        CharacterController.enabled = true;

        CharacterController.Move(movement);

        transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);

        Debug.Log($"GetNextFrameData for InputTick: {inputData.InputTick}, {inputData.MovementAxes} => {currentStateData.Position}, {transform.localPosition}");

        return new PlayerStateData(currentStateData.Id, inputData.InputTick, transform.localPosition, rotation);
    }
}

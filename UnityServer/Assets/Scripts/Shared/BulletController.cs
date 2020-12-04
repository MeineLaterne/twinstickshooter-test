using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BulletController : MonoBehaviour {

    internal event System.Action<Vector3> VelocityChanged;
    
    [SerializeField] private float movementSpeed;

    private CharacterController characterController;

    private Vector3 velocity;

    internal void ResetTo(BulletStateData stateData) {
        characterController.enabled = false;

        transform.localPosition = stateData.Position;

        characterController.enabled = true;
    }

    public BulletStateData GetNextFrameData(BulletInputData inputData, BulletStateData currentState) {
        velocity = new Vector3(inputData.MovementAxes.x, 0, inputData.MovementAxes.y) * movementSpeed * Time.fixedDeltaTime;
        
        characterController.Move(velocity);
        
        return new BulletStateData(currentState.Id, currentState.PlayerId, inputData.InputTick, transform.localPosition);
    }

    private void Awake() {
        characterController = GetComponent<CharacterController>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        if (hit.collider.CompareTag("Obstacle")) {
            velocity = velocity.Bounce(hit.normal);
            VelocityChanged?.Invoke(velocity);
        }
    }

}

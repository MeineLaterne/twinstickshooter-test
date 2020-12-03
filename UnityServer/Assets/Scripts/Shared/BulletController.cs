using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BulletController : MonoBehaviour {

    public Vector3 Velocity { get; set; }

    private uint frame;
    private CharacterController characterController;

    public void ResetTo(BulletStateData stateData) {
        characterController.enabled = false;

        transform.localPosition = stateData.Position;

        characterController.enabled = true;
    }

    public BulletStateData GetNextFrameData(BulletStateData currentState) {
        frame++;
        characterController.Move(Velocity * Time.fixedDeltaTime);
        return new BulletStateData(currentState.Id, currentState.PlayerId, frame, transform.localPosition);
    }

    private void Awake() {
        characterController = GetComponent<CharacterController>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        if (hit.collider.CompareTag("Obstacle")) {
            Velocity = Velocity.Bounce(hit.normal);
        }
    }

}

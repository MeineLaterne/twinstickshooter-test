using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BulletController : MonoBehaviour {

    public Vector3 Velocity { get; set; }

    [SerializeField] private float speed = 40f;
    
    private CharacterController characterController;

    public BulletStateData GetNextFrameData(BulletStateData currentState) {
        characterController.Move(Velocity * Time.fixedDeltaTime);
        return new BulletStateData(currentState.Id, transform.position);
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

using UnityEngine;

[RequireComponent(typeof(BulletController))]
public class ServerBullet : MonoBehaviour { 
    
    public ushort Id { get; private set; }
    public ushort PlayerId { get; private set; }
    public ServerPlayer Owner { get; private set; }
    public float Speed => speed;
    public BulletStateData BulletState { get; private set; }

    [SerializeField] private float speed;

    private BulletController bulletController;
    

    public void Initialize(ServerPlayer owner, BulletSpawnData spawnData) {
        Id = spawnData.Id;
        PlayerId = spawnData.PlayerId;
        BulletState = new BulletStateData(Id, PlayerId, spawnData.Position);
        Owner = owner;
        transform.position = spawnData.Position;
        bulletController.Velocity = spawnData.Velocity;
        GetComponent<CharacterController>().enabled = true;
    }

    public BulletStateData BulletUpdate() {
        BulletState = bulletController.GetNextFrameData(BulletState);
        return BulletState;
    }

    private void Awake() {
        bulletController = GetComponent<BulletController>();
    }

    private void OnDisable() {
        Owner = null;
        bulletController.Velocity = Vector3.zero;
        GetComponent<CharacterController>().enabled = false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        if (!hit.collider.CompareTag("Obstacle")) {
            Owner.Room.DespawnBullet(this);
            Debug.Log($"bullet {Id} hit {hit.collider.gameObject.name}");
        }
    }

}
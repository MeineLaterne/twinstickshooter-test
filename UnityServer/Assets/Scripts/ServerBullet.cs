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
    

    public void Initialize(ushort id, ushort playerId, ServerPlayer owner) {
        Id = id;
        PlayerId = playerId;
        Owner = owner;
        transform.localPosition = new Vector3(10000, 10000, 10000);
    }

    public void Go(BulletSpawnData spawnData) {
        BulletState = new BulletStateData(Id, PlayerId, 0, spawnData.Position);
        transform.localPosition = spawnData.Position;
        bulletController.Velocity = spawnData.Velocity;
        GetComponent<CharacterController>().enabled = true;
    }

    public BulletStateData BulletUpdate() {
        BulletState = bulletController.GetNextFrameData(BulletState);
        transform.localPosition = BulletState.Position;
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
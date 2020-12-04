using UnityEngine;

[RequireComponent(typeof(BulletController))]
public class ServerBullet : MonoBehaviour { 
    
    public ushort Id { get; private set; }
    public ushort PlayerId { get; private set; }
    public uint InputTick { get; private set; }
    public ServerPlayer Owner { get; private set; }
    public BulletStateData BulletState { get; private set; }

    private BulletController bulletController;

    private readonly QueueBuffer<BulletInputData> inputBuffer = new QueueBuffer<BulletInputData>(1);
    private BulletInputData[] inputsToProcess;
    public void Initialize(ushort id, ushort playerId, ServerPlayer owner) {
        Id = id;
        PlayerId = playerId;
        Owner = owner;
        transform.localPosition = new Vector3(10000, 10000, 10000);
    }

    public void Go(BulletSpawnData spawnData) {
        BulletState = new BulletStateData(Id, PlayerId, 0, spawnData.Position);
        transform.localPosition = spawnData.Position;

        Owner.AddBullet(this);

        GetComponent<CharacterController>().enabled = true;
    }

    public BulletStateData BulletUpdate() {
        inputsToProcess = inputBuffer.Get();
        foreach (var input in inputsToProcess) {
            InputTick = input.InputTick;
            BulletState = bulletController.GetNextFrameData(input, BulletState);
        }
        
        transform.localPosition = BulletState.Position;
        return BulletState;
    }

    public void ReceiveInput(BulletInputData inputData) => inputBuffer.Add(inputData);

    private void Awake() {
        bulletController = GetComponent<BulletController>();
    }

    private void OnDisable() {
        if (Owner != null) {
            Owner.RemoveBullet(Id);
            Owner = null;
        }
        GetComponent<CharacterController>().enabled = false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        if (!hit.collider.CompareTag("Obstacle")) {
            Owner.RemoveBullet(Id);
            Owner.Room.DespawnBullet(this);
            Debug.Log($"bullet {Id} hit {hit.collider.gameObject.name}");
        }
    }

}
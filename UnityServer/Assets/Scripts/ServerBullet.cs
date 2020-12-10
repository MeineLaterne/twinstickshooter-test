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
        //Debug.Log($"bullet {Id} Initialize");
        Id = id;
        PlayerId = playerId;
        Owner = owner;
        transform.localPosition = new Vector3(10000, 10000, 10000);
    }

    public BulletStateData Go(BulletSpawnData spawnData) {
        //Debug.Log($"bullet {Id} Go");

        BulletState = new BulletStateData(Id, PlayerId, 0, spawnData.Position);
        bulletController.ResetTo(BulletState);

        Owner.AddBullet(this);
        
        return BulletState;
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
        //Debug.Log($"bullet {Id} OnDisable");
        if (Owner != null) {
            Owner.RemoveBullet(Id);
            Owner = null;
        }
        inputBuffer.Clear();
    }

    private void Disable() {
        //Debug.Log($"bullet {Id} Disable");
        Owner.RemoveBullet(Id);
        Owner.Room.DespawnBullet(this);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {

        if (hit.collider.CompareTag("Bullet")) {
            var otherBullet = hit.collider.gameObject.GetComponent<ServerBullet>();
            //Debug.Log($"bullet {Id} hit {otherBullet.Id}");
            otherBullet.Disable();
        }

        if (!hit.collider.CompareTag("Obstacle")) {
            Disable();
        }

    }

}
using UnityEngine;

class DefaultBulletInputReader : MonoBehaviour, IInputReader<BulletInputData> {
    public uint InputTick { get; private set; }

    //private ushort bulletId;
    //private Vector3 lastPosition;
    //private Vector3 direction;

    private ClientBullet clientBullet;

    public BulletInputData ReadInput() {
        InputTick++;
        return new BulletInputData(clientBullet.Id, InputTick, new Vector2(clientBullet.Direction.x, clientBullet.Direction.z));
    }

    private void Start() {
        clientBullet = GetComponent<ClientBullet>();
    }

    //private void Update() {
    //    direction = transform.position - lastPosition;
    //    lastPosition = transform.position;
    //}
}
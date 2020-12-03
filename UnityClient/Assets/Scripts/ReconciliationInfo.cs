
struct ReconciliationInfo {
    public uint InputTick;
    public PlayerStateData StateData;
    public PlayerInputData InputData;

    public ReconciliationInfo(uint inputTick, PlayerStateData stateData, PlayerInputData inputData) {
        InputTick = inputTick;
        StateData = stateData;
        InputData = inputData;
    }
}

struct BulletReconciliationInfo {
    public uint Frame;
    public BulletStateData StateData;

    public BulletReconciliationInfo(uint frame, BulletStateData stateData) {
        Frame = frame;
        StateData = stateData;
    }
}

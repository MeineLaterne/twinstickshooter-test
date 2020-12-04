
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
    public uint InputTick;
    public BulletStateData StateData;
    public BulletInputData InputData;

    public BulletReconciliationInfo(uint inputTick, BulletStateData stateData, BulletInputData inputData) {
        InputTick = inputTick;
        StateData = stateData;
        InputData = inputData;
    }
}

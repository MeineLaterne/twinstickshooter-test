
struct ReconciliationInfo {
    public uint Frame;
    public PlayerStateData StateData;
    public PlayerInputData InputData;

    public ReconciliationInfo(uint frame, PlayerStateData stateData, PlayerInputData inputData) {
        Frame = frame;
        StateData = stateData;
        InputData = inputData;
    }
}
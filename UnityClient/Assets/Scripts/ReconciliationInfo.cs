
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

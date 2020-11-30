public interface IInputReader {
    uint InputTick { get; }
    PlayerInputData ReadInput();
}

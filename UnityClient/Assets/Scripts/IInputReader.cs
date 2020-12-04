public interface IInputReader<T> {
    uint InputTick { get; }
    T ReadInput();
}

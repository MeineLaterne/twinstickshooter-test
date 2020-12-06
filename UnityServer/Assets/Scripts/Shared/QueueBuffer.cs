using System.Collections.Generic;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class QueueBuffer<T> {

    private readonly Queue<T> elements = new Queue<T>();
    private readonly int bufferSize;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bufferSize">
    /// bufferSize is the ideal size that the buffer should have, 
    /// the buffer will try to always keep that many elements in it. 
    /// If we increase this value we add delay to the execution of messages but reduce the chance of getting jitter. 
    /// Increasing bufferSize by one will add a delay of FixedDeltaTime (in our case 25 ms because we have 40 FixedUpdates per second) 
    /// but will also allow the ping of the player to bounce between twice that amount.</param>
    public QueueBuffer(int bufferSize) {
        this.bufferSize = bufferSize;
    }

    public int Count => elements.Count;

    public void Add(T element) => elements.Enqueue(element);

    public void Clear() => elements.Clear();

    public T[] Get() {
        if (elements.Count - 1 < bufferSize) {
            return new T[0];
        }

        var amount = elements.Count - bufferSize;
        var r = new T[amount];

        for (var i = 0; i < amount; i++) {
            r[i] = elements.Dequeue();
        }

        return r;
    }

}

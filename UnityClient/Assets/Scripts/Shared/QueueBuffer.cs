using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class QueueBuffer<T> {
    private readonly Queue<T> queue = new Queue<T>();
    private readonly int bufferSize;
    private readonly int tolerance;
    private int counter;

    public QueueBuffer(int bufferSize, int tolerance = 1) {
        this.bufferSize = bufferSize;
        this.tolerance = tolerance;
    }

    public int Count => queue.Count;

    public void Add(T element) => queue.Enqueue(element);

}

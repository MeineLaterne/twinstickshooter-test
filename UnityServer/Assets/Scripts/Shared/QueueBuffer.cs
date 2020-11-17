﻿using System.Collections.Generic;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class QueueBuffer<T> {

    public static T[] Empty => new T[0];

    private readonly Queue<T> elements = new Queue<T>();
    private readonly int bufferSize;
    private readonly int tolerance;
    private int counter;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bufferSize">
    /// bufferSize is the ideal size that the buffer should have, 
    /// the buffer will try to always keep that many elements in it. 
    /// If we increase this value we add delay to the execution of messages but reduce the chance of getting jitter. 
    /// Increasing bufferSize by one will add a delay of FixedDeltaTime (in our case 25 ms because we have 40 FixedUpdates per second) 
    /// but will also allow the ping of the player to bounce between twice that amount.</param>
    /// <param name="tolerance">used to determine whether one ore more elements should be flushed from the buffer when calling buffer.Get()</param>
    public QueueBuffer(int bufferSize, int tolerance = 1) {
        this.bufferSize = bufferSize;
        this.tolerance = tolerance;
    }

    public int Count => elements.Count;

    public void Add(T element) => elements.Enqueue(element);

    public T[] Get() {
        var size = elements.Count - 1;

        if (size == bufferSize) {
            counter = 0;
        }

        if (size > bufferSize) {
            if (counter < 0) {
                counter = 0;
            }
            counter++;
            if (counter > tolerance) {
                var amount = elements.Count - bufferSize;
                var r = new T[amount];
                for (int i = 0; i < amount; i++) {
                    r[i] = elements.Dequeue();
                }
                return r;
            }
        }

        if (size < bufferSize) {
            if (counter > 0) {
                counter = 0;
            }
            counter--;
            if (-counter > tolerance) {
                return Empty;
            }
        }

        if (elements.Count > 0) {
            return new T[] { elements.Dequeue() };
        }

        return Empty;
    }

}

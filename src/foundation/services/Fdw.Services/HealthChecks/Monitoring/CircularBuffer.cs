using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Thread-safe circular buffer with a fixed capacity. When full, the oldest item is overwritten.
/// </summary>
/// <typeparam name="T">The type of items stored in the buffer.</typeparam>
internal sealed class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private readonly Lock _lock = new();
    private int _head;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of items the buffer can hold.</param>
    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    /// <summary>
    /// Gets the current number of items in the buffer.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// Adds an item to the buffer. If the buffer is full, the oldest item is overwritten.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        lock (_lock)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length)
            {
                _count++;
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of all items in the buffer, ordered from oldest to newest.
    /// </summary>
    /// <returns>A list of items in chronological order.</returns>
    public List<T> GetItems()
    {
        lock (_lock)
        {
            var items = new List<T>(_count);
            if (_count == 0)
            {
                return items;
            }

            var start = _count < _buffer.Length ? 0 : _head;
            for (var i = 0; i < _count; i++)
            {
                items.Add(_buffer[(start + i) % _buffer.Length]);
            }

            return items;
        }
    }
}

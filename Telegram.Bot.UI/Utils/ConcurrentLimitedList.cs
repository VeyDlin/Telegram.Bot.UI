namespace Telegram.Bot.UI.Utils;

/// <summary>
/// Thread-safe limited-size list that automatically evicts oldest items when capacity is reached.
/// Implements LRU (Least Recently Used) behavior for existing items.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class ConcurrentLimitedList<T> {
    private readonly object sync = new();
    private int maxItems { get; }
    private List<T> list;

    /// <summary>
    /// Initializes a new instance with the specified maximum capacity.
    /// </summary>
    /// <param name="maxItems">The maximum number of items the list can hold.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxItems is less than or equal to 0.</exception>
    public ConcurrentLimitedList(int maxItems) {
        if (maxItems <= 0) {
            throw new ArgumentOutOfRangeException();
        }

        this.maxItems = maxItems;
        list = new List<T>(this.maxItems);
    }

    /// <summary>
    /// Adds an item to the list. If the item exists, it's moved to the end (most recently used).
    /// If the list is full, the oldest item is evicted.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>A list of evicted items (empty if no items were removed).</returns>
    public List<T> Add(T item) {
        lock (sync) {
            var removed = new List<T>();

            var existingIndex = list.IndexOf(item);
            if (existingIndex >= 0) {
                list.RemoveAt(existingIndex);
                list.Add(item);
                return removed;
            }

            if (list.Count == maxItems) {
                removed.Add(list[0]);
                list.RemoveAt(0);
            }
            list.Add(item);
            return removed;
        }
    }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index] {
        get {
            lock (sync) {
                return list[index];
            }
        }
    }

    /// <summary>
    /// Gets the number of elements currently in the list.
    /// </summary>
    public int count {
        get {
            lock (sync) {
                return list.Count;
            }
        }
    }

    /// <summary>
    /// Returns the first element in the list.
    /// </summary>
    /// <returns>The first element.</returns>
    public T First() {
        lock (sync) {
            return list.First();
        }
    }

    /// <summary>
    /// Returns the last element in the list.
    /// </summary>
    /// <returns>The last element.</returns>
    public T Last() {
        lock (sync) {
            return list.Last();
        }
    }

    /// <summary>
    /// Returns the last element in the list, or default if the list is empty.
    /// </summary>
    /// <returns>The last element or default value.</returns>
    public T? LastOrDefault() {
        lock (sync) {
            return list.LastOrDefault();
        }
    }

    /// <summary>
    /// Copies the list elements to a new array.
    /// </summary>
    /// <returns>An array containing copies of the elements.</returns>
    public T[] ToArray() {
        lock (sync) {
            return list.ToArray();
        }
    }

    /// <summary>
    /// Removes all elements from the list.
    /// </summary>
    public void Clear() {
        lock (sync) {
            list.Clear();
        }
    }

    /// <summary>
    /// Determines whether the list contains any elements.
    /// </summary>
    /// <returns>True if the list contains elements, false otherwise.</returns>
    public bool Any() {
        lock (sync) {
            return list.Any();
        }
    }
}
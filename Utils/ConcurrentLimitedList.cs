namespace Telegram.Bot.UI.Utils;


public class ConcurrentLimitedList<T> {
    private readonly object sync = new();
    private int maxItems { get; }
    private List<T> list;



    public ConcurrentLimitedList(int maxItems) {
        if (maxItems <= 0) {
            throw new ArgumentOutOfRangeException();
        }

        this.maxItems = maxItems;
        list = new List<T>(this.maxItems);
    }



    public void Add(T item) {
        lock (sync) {
            if (list.Count == maxItems) {
                list.RemoveAt(0);
            }
            list.Add(item);
        }
    }



    public T this[int index] {
        get {
            lock (sync) {
                return list[index];
            }
        }
    }



    public int count {
        get {
            lock (sync) {
                return list.Count;
            }
        }
    }



    public T First() {
        lock (sync) {
            return list.First();
        }
    }



    public T Last() {
        lock (sync) {
            return list.Last();
        }
    }



    public T? LastOrDefault() {
        lock (sync) {
            return list.LastOrDefault();
        }
    }



    public T[] ToArray() {
        lock (sync) {
            return list.ToArray();
        }
    }



    public void Clear() {
        lock (sync) {
            list.Clear();
        }
    }



    public bool Any() {
        lock (sync) {
            return list.Any();
        }
    }
}
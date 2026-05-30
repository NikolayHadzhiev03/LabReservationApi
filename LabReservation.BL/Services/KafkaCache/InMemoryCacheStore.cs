namespace LabReservation.BL.Services.KafkaCache
{
    /// <summary>
    /// Copy-on-write in-memory cache. Writes (ReplaceAll/Upsert) build a new dictionary and swap
    /// the reference under a lock, so reads (TryGet/GetAll) are lock-free and always see a
    /// consistent, immutable snapshot. The key for each item is supplied by <paramref name="keySelector"/>.
    /// </summary>
    public class InMemoryCacheStore<T> : ICacheStore<T>
    {
        private readonly Func<T, string> _keySelector;
        private readonly object _writeLock = new();
        private volatile Dictionary<string, T> _items = new();

        public InMemoryCacheStore(Func<T, string> keySelector)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        public void ReplaceAll(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var next = new Dictionary<string, T>();
            foreach (var item in items)
            {
                next[_keySelector(item)] = item;
            }

            lock (_writeLock)
            {
                _items = next;
            }
        }

        public void Upsert(T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var key = _keySelector(item);
            lock (_writeLock)
            {
                _items = new Dictionary<string, T>(_items) { [key] = item };
            }
        }

        public bool TryGet(string id, out T? item)
        {
            if (id != null && _items.TryGetValue(id, out var found))
            {
                item = found;
                return true;
            }

            item = default;
            return false;
        }

        public IReadOnlyCollection<T> GetAll() => _items.Values.ToList();

        public int Count => _items.Count;
    }
}

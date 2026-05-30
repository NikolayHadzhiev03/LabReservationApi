namespace LabReservation.BL.Services.KafkaCache
{
    /// <summary>
    /// In-memory cache of items keyed by id. Populated from Kafka snapshots and read by
    /// services instead of hitting the database. Implementations must be thread-safe.
    /// </summary>
    public interface ICacheStore<T>
    {
        /// <summary>Replace the entire cache contents with the given items (applied from a full snapshot).</summary>
        void ReplaceAll(IEnumerable<T> items);

        /// <summary>Add or update a single item.</summary>
        void Upsert(T item);

        /// <summary>Look up a single item by its id.</summary>
        bool TryGet(string id, out T? item);

        /// <summary>Return a point-in-time copy of all cached items.</summary>
        IReadOnlyCollection<T> GetAll();

        /// <summary>Number of items currently cached.</summary>
        int Count { get; }
    }
}

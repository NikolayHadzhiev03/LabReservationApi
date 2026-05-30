using LabReservation.Models.Entities;

namespace LabReservation.Models.Caching
{
    /// <summary>
    /// Full snapshot of the labs collection, published periodically by the DbCacheReader
    /// to the cache topic and applied wholesale to the in-memory KafkaCache by consumers.
    /// </summary>
    public record LabsSnapshotEvent(IReadOnlyList<Lab> Labs, DateTime SnapshotAt);

    /// <summary>
    /// Full snapshot of the reservations collection (see <see cref="LabsSnapshotEvent"/>).
    /// </summary>
    public record ReservationsSnapshotEvent(IReadOnlyList<Reservation> Reservations, DateTime SnapshotAt);
}

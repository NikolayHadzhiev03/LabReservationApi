using LabReservation.Models.Entities;

namespace LabReservation.Models.Events
{
    public record ReservationCreatedEvent(
        string ReservationId,
        string LabId,
        string CustomerName,
        string CustomerEmail,
        DateTime StartTime,
        DateTime EndTime,
        string Purpose,
        ReservationStatus Status,
        DateTime CreatedAt);

    public record ReservationUpdatedEvent(
        string ReservationId,
        string LabId,
        DateTime StartTime,
        DateTime EndTime,
        string Purpose,
        ReservationStatus Status,
        DateTime UpdatedAt);

    public record ReservationDeletedEvent(
        string ReservationId,
        DateTime DeletedAt);

    public record ReservationCancelledEvent(
        string ReservationId,
        string LabId,
        string CustomerEmail,
        DateTime CancelledAt);

    public record ReservationConfirmedEvent(
        string ReservationId,
        string LabId,
        string CustomerEmail,
        DateTime ConfirmedAt);
}

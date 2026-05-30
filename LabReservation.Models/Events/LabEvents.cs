namespace LabReservation.Models.Events
{
    public record LabCreatedEvent(
        string LabId,
        string Name,
        string Location,
        int Capacity,
        IReadOnlyList<string> Equipment,
        bool IsAvailable,
        DateTime CreatedAt);

    public record LabUpdatedEvent(
        string LabId,
        string Name,
        string Location,
        int Capacity,
        IReadOnlyList<string> Equipment,
        bool IsAvailable,
        DateTime UpdatedAt);

    public record LabDeletedEvent(
        string LabId,
        DateTime DeletedAt);
}

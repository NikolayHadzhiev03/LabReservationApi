using LabReservation.Models.Entities;

namespace LabReservation.DataLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Reservation entity operations
    /// </summary>
    public interface IReservationRepository
    {
        Task<Reservation?> GetByIdAsync(string id);
        Task<IEnumerable<Reservation>> GetAllAsync();
        Task<IEnumerable<Reservation>> GetByLabIdAsync(string labId);
   Task<IEnumerable<Reservation>> GetByCustomerEmailAsync(string email);
 Task<IEnumerable<Reservation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Reservation>> GetConflictingReservationsAsync(string labId, DateTime startTime, DateTime endTime, string? excludeReservationId = null);
        Task<Reservation> CreateAsync(Reservation reservation);
        Task<Reservation?> UpdateAsync(string id, Reservation reservation);
 Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
        Task<int> GetCountByLabIdAsync(string labId);
        Task<int> GetCountByStatusAsync(string labId, ReservationStatus status);
    }
}

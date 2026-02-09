using LabReservation.Models.Responses;

namespace LabReservation.BL.Services.Interfaces
{
   /// <summary>
    /// Business service interface with 2 injected interfaces (ILabService and IReservationService)
    /// Contains complex business logic combining lab and reservation operations
    /// </summary>
    public interface ILabReservationBusinessService
    {
        /// <summary>
        /// Check if a lab is available for a specific time slot
     /// </summary>
        Task<LabAvailabilityResponse> CheckLabAvailabilityAsync(string labId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Get reservation summary/statistics for a specific lab
      /// </summary>
    Task<ReservationSummaryResponse> GetLabReservationSummaryAsync(string labId);
   }
}

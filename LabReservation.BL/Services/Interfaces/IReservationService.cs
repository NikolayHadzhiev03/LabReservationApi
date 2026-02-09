using LabReservation.Models.DTO;
using LabReservation.Models.Entities;
using LabReservation.Models.Requests;

namespace LabReservation.BL.Services.Interfaces
{
    /// <summary>
    /// Service interface for Reservation CRUD operations
    /// </summary>
    public interface IReservationService
    {
   Task<ReservationDto?> GetByIdAsync(string id);
        Task<IEnumerable<ReservationDto>> GetAllAsync();
    Task<IEnumerable<ReservationDto>> GetByLabIdAsync(string labId);
        Task<IEnumerable<ReservationDto>> GetByCustomerEmailAsync(string email);
      Task<ReservationDto> CreateAsync(CreateReservationRequest request);
      Task<ReservationDto?> UpdateAsync(string id, UpdateReservationRequest request);
        Task<bool> DeleteAsync(string id);
        Task<bool> CancelReservationAsync(string id);
        Task<bool> ConfirmReservationAsync(string id);
 }
}

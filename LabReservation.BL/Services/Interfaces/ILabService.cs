using LabReservation.Models.DTO;
using LabReservation.Models.Entities;
using LabReservation.Models.Requests;

namespace LabReservation.BL.Services.Interfaces
{
    /// <summary>
    /// Service interface for Lab CRUD operations
/// </summary>
    public interface ILabService
    {
        Task<LabDto?> GetByIdAsync(string id);
        Task<IEnumerable<LabDto>> GetAllAsync();
        Task<IEnumerable<LabDto>> GetAvailableLabsAsync();
     Task<LabDto> CreateAsync(AddLabRequest request);
        Task<LabDto?> UpdateAsync(string id, UpdateLabRequest request);
        Task<bool> DeleteAsync(string id);
  }
}

using LabReservation.Models.Entities;

namespace LabReservation.DataLayer.Repositories.Interfaces
{
  /// <summary>
    /// Repository interface for Lab entity operations
    /// </summary>
  public interface ILabRepository
    {
  Task<Lab?> GetByIdAsync(string id);
 Task<IEnumerable<Lab>> GetAllAsync();
    Task<IEnumerable<Lab>> GetAvailableLabsAsync();
        Task<Lab> CreateAsync(Lab lab);
        Task<Lab?> UpdateAsync(string id, Lab lab);
        Task<bool> DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}

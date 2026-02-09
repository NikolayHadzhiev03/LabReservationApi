using LabReservation.BL.Services.Interfaces;
using LabReservation.DataLayer.Repositories.Interfaces;
using LabReservation.Models.DTO;
using LabReservation.Models.Entities;
using LabReservation.Models.Requests;
using Mapster;
using Microsoft.Extensions.Logging;

namespace LabReservation.BL.Services.Implementations
{
    /// <summary>
    /// Implementation of Lab service with CRUD operations
    /// </summary>
    public class LabService : ILabService
    {
        private readonly ILabRepository _labRepository;
    private readonly ILogger<LabService> _logger;

        public LabService(ILabRepository labRepository, ILogger<LabService> logger)
        {
      _labRepository = labRepository;
_logger = logger;
    }

   public async Task<LabDto?> GetByIdAsync(string id)
        {
   _logger.LogInformation("Getting lab with ID: {LabId}", id);
     var lab = await _labRepository.GetByIdAsync(id);
       
     if (lab == null)
       {
             _logger.LogWarning("Lab with ID {LabId} not found", id);
            return null;
            }

   return lab.Adapt<LabDto>();
        }

        public async Task<IEnumerable<LabDto>> GetAllAsync()
     {
   _logger.LogInformation("Getting all labs");
            var labs = await _labRepository.GetAllAsync();
            return labs.Adapt<IEnumerable<LabDto>>();
  }

        public async Task<IEnumerable<LabDto>> GetAvailableLabsAsync()
        {
       _logger.LogInformation("Getting available labs");
        var labs = await _labRepository.GetAvailableLabsAsync();
   return labs.Adapt<IEnumerable<LabDto>>();
        }

        public async Task<LabDto> CreateAsync(AddLabRequest request)
        {
    _logger.LogInformation("Creating new lab: {LabName}", request.Name);
     
            var lab = request.Adapt<Lab>();
            lab.IsAvailable = true;
        
  var createdLab = await _labRepository.CreateAsync(lab);
        _logger.LogInformation("Lab created successfully with ID: {LabId}", createdLab.Id);
        
   return createdLab.Adapt<LabDto>();
}

   public async Task<LabDto?> UpdateAsync(string id, UpdateLabRequest request)
{
     _logger.LogInformation("Updating lab with ID: {LabId}", id);
         
         var existingLab = await _labRepository.GetByIdAsync(id);
            if (existingLab == null)
       {
                _logger.LogWarning("Lab with ID {LabId} not found for update", id);
       return null;
  }

     existingLab.Name = request.Name;
 existingLab.Location = request.Location;
        existingLab.Capacity = request.Capacity;
    existingLab.Equipment = request.Equipment;
         existingLab.IsAvailable = request.IsAvailable;
        
        var updatedLab = await _labRepository.UpdateAsync(id, existingLab);
         
    if (updatedLab == null)
{
  _logger.LogError("Failed to update lab with ID: {LabId}", id);
        return null;
        }

            _logger.LogInformation("Lab updated successfully: {LabId}", id);
   return updatedLab.Adapt<LabDto>();
        }

        public async Task<bool> DeleteAsync(string id)
        {
  _logger.LogInformation("Deleting lab with ID: {LabId}", id);
       var result = await _labRepository.DeleteAsync(id);
  
            if (result)
    {
         _logger.LogInformation("Lab deleted successfully: {LabId}", id);
  }
   else
        {
     _logger.LogWarning("Lab with ID {LabId} not found for deletion", id);
    }
       
 return result;
  }
    }
}

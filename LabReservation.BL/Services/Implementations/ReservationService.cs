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
    /// Implementation of Reservation service with CRUD operations
    /// </summary>
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ILabRepository _labRepository;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
         IReservationRepository reservationRepository,
       ILabRepository labRepository,
           ILogger<ReservationService> logger)
        {
   _reservationRepository = reservationRepository;
  _labRepository = labRepository;
   _logger = logger;
 }

        public async Task<ReservationDto?> GetByIdAsync(string id)
   {
      _logger.LogInformation("Getting reservation with ID: {ReservationId}", id);
    var reservation = await _reservationRepository.GetByIdAsync(id);
            
       if (reservation == null)
            {
    _logger.LogWarning("Reservation with ID {ReservationId} not found", id);
   return null;
            }

            var dto = reservation.Adapt<ReservationDto>();
        
            // Get lab name for display
   var lab = await _labRepository.GetByIdAsync(reservation.LabId);
       dto.LabName = lab?.Name ?? "Unknown Lab";
     
    return dto;
        }

  public async Task<IEnumerable<ReservationDto>> GetAllAsync()
        {
   _logger.LogInformation("Getting all reservations");
  var reservations = await _reservationRepository.GetAllAsync();
   var dtos = new List<ReservationDto>();
     
    foreach (var reservation in reservations)
    {
            var dto = reservation.Adapt<ReservationDto>();
    var lab = await _labRepository.GetByIdAsync(reservation.LabId);
             dto.LabName = lab?.Name ?? "Unknown Lab";
      dtos.Add(dto);
  }
            
            return dtos;
    }

     public async Task<IEnumerable<ReservationDto>> GetByLabIdAsync(string labId)
        {
     _logger.LogInformation("Getting reservations for lab: {LabId}", labId);
  var reservations = await _reservationRepository.GetByLabIdAsync(labId);
            var lab = await _labRepository.GetByIdAsync(labId);
     var labName = lab?.Name ?? "Unknown Lab";
      
  return reservations.Select(r =>
    {
var dto = r.Adapt<ReservationDto>();
        dto.LabName = labName;
          return dto;
            });
        }

        public async Task<IEnumerable<ReservationDto>> GetByCustomerEmailAsync(string email)
     {
            _logger.LogInformation("Getting reservations for customer: {Email}", email);
       var reservations = await _reservationRepository.GetByCustomerEmailAsync(email);
      var dtos = new List<ReservationDto>();
   
            foreach (var reservation in reservations)
{
            var dto = reservation.Adapt<ReservationDto>();
         var lab = await _labRepository.GetByIdAsync(reservation.LabId);
                dto.LabName = lab?.Name ?? "Unknown Lab";
         dtos.Add(dto);
   }
            
  return dtos;
        }

        public async Task<ReservationDto> CreateAsync(CreateReservationRequest request)
        {
   _logger.LogInformation("Creating reservation for lab {LabId} by {CustomerName}", request.LabId, request.CustomerName);
     
      // Verify lab exists
       var lab = await _labRepository.GetByIdAsync(request.LabId);
   if (lab == null)
            {
          _logger.LogError("Lab {LabId} not found", request.LabId);
       throw new InvalidOperationException($"Lab with ID {request.LabId} not found");
  }

            // Check for conflicting reservations
      var conflicts = await _reservationRepository.GetConflictingReservationsAsync(
    request.LabId, request.StartTime, request.EndTime);
      
            if (conflicts.Any())
    {
    _logger.LogWarning("Conflicting reservations found for lab {LabId}", request.LabId);
       throw new InvalidOperationException("The requested time slot conflicts with existing reservations");
     }

            var reservation = request.Adapt<Reservation>();
       reservation.Status = ReservationStatus.Pending;
          
    var createdReservation = await _reservationRepository.CreateAsync(reservation);
       _logger.LogInformation("Reservation created with ID: {ReservationId}", createdReservation.Id);
            
var dto = createdReservation.Adapt<ReservationDto>();
          dto.LabName = lab.Name;
        
            return dto;
   }

   public async Task<ReservationDto?> UpdateAsync(string id, UpdateReservationRequest request)
        {
   _logger.LogInformation("Updating reservation: {ReservationId}", id);
       
     var existingReservation = await _reservationRepository.GetByIdAsync(id);
            if (existingReservation == null)
            {
                _logger.LogWarning("Reservation {ReservationId} not found", id);
       return null;
            }

          // Check for conflicts if time changed
    if (existingReservation.StartTime != request.StartTime || existingReservation.EndTime != request.EndTime)
       {
       var conflicts = await _reservationRepository.GetConflictingReservationsAsync(
           existingReservation.LabId, request.StartTime, request.EndTime, id);
  
      if (conflicts.Any())
     {
      _logger.LogWarning("Update would cause conflict for reservation {ReservationId}", id);
      throw new InvalidOperationException("The requested time slot conflicts with existing reservations");
             }
  }

    existingReservation.StartTime = request.StartTime;
     existingReservation.EndTime = request.EndTime;
 existingReservation.Purpose = request.Purpose;
          existingReservation.Status = request.Status;
            
var updatedReservation = await _reservationRepository.UpdateAsync(id, existingReservation);
       
 if (updatedReservation == null)
    {
 _logger.LogError("Failed to update reservation {ReservationId}", id);
      return null;
            }

var dto = updatedReservation.Adapt<ReservationDto>();
      var lab = await _labRepository.GetByIdAsync(updatedReservation.LabId);
      dto.LabName = lab?.Name ?? "Unknown Lab";
            
 _logger.LogInformation("Reservation updated: {ReservationId}", id);
         return dto;
        }

        public async Task<bool> DeleteAsync(string id)
   {
        _logger.LogInformation("Deleting reservation: {ReservationId}", id);
          var result = await _reservationRepository.DeleteAsync(id);
       
       if (result)
      {
      _logger.LogInformation("Reservation deleted: {ReservationId}", id);
         }
    else
    {
     _logger.LogWarning("Reservation {ReservationId} not found for deletion", id);
         }

return result;
        }

   public async Task<bool> CancelReservationAsync(string id)
 {
 _logger.LogInformation("Cancelling reservation: {ReservationId}", id);
    
     var reservation = await _reservationRepository.GetByIdAsync(id);
        if (reservation == null)
{
                _logger.LogWarning("Reservation {ReservationId} not found for cancellation", id);
   return false;
            }

       reservation.Status = ReservationStatus.Cancelled;
    var result = await _reservationRepository.UpdateAsync(id, reservation);
            
    if (result != null)
    {
    _logger.LogInformation("Reservation cancelled: {ReservationId}", id);
      return true;
       }

      return false;
        }

        public async Task<bool> ConfirmReservationAsync(string id)
        {
       _logger.LogInformation("Confirming reservation: {ReservationId}", id);
            
         var reservation = await _reservationRepository.GetByIdAsync(id);
   if (reservation == null)
            {
       _logger.LogWarning("Reservation {ReservationId} not found for confirmation", id);
     return false;
            }

        reservation.Status = ReservationStatus.Confirmed;
     var result = await _reservationRepository.UpdateAsync(id, reservation);
 
            if (result != null)
            {
      _logger.LogInformation("Reservation confirmed: {ReservationId}", id);
     return true;
    }

      return false;
        }
    }
}

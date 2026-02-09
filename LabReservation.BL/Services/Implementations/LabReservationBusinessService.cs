using LabReservation.BL.Services.Interfaces;
using LabReservation.DataLayer.Repositories.Interfaces;
using LabReservation.Models.DTO;
using LabReservation.Models.Entities;
using LabReservation.Models.Responses;
using Mapster;
using Microsoft.Extensions.Logging;

namespace LabReservation.BL.Services.Implementations
{
    /// <summary>
    /// Business service implementation with 2 injected interfaces (ILabRepository and IReservationRepository)
  /// Contains complex business logic combining lab and reservation operations
    /// </summary>
    public class LabReservationBusinessService : ILabReservationBusinessService
    {
        private readonly ILabRepository _labRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ILogger<LabReservationBusinessService> _logger;

        public LabReservationBusinessService(
            ILabRepository labRepository,
          IReservationRepository reservationRepository,
          ILogger<LabReservationBusinessService> logger)
   {
     _labRepository = labRepository;
   _reservationRepository = reservationRepository;
       _logger = logger;
        }

        /// <summary>
        /// Check if a lab is available for a specific time slot
     /// Business logic: checks lab existence, availability status, and conflicting reservations
        /// </summary>
        public async Task<LabAvailabilityResponse> CheckLabAvailabilityAsync(string labId, DateTime startTime, DateTime endTime)
        {
 _logger.LogInformation("Checking availability for lab {LabId} from {Start} to {End}", 
       labId, startTime, endTime);

            var response = new LabAvailabilityResponse
            {
      LabId = labId
          };

          // Get the lab
   var lab = await _labRepository.GetByIdAsync(labId);
      if (lab == null)
            {
  _logger.LogWarning("Lab {LabId} not found", labId);
        response.IsAvailable = false;
       response.Message = "Lab not found";
            return response;
       }

            response.LabName = lab.Name;

          // Check if lab is marked as available
    if (!lab.IsAvailable)
       {
      _logger.LogInformation("Lab {LabId} is marked as unavailable", labId);
   response.IsAvailable = false;
                response.Message = "Lab is currently not available for reservations";
    return response;
          }

       // Check for conflicting reservations
          var conflicts = await _reservationRepository.GetConflictingReservationsAsync(labId, startTime, endTime);
            var conflictList = conflicts.ToList();

  if (conflictList.Any())
     {
          _logger.LogInformation("Found {Count} conflicting reservations for lab {LabId}", conflictList.Count, labId);
     response.IsAvailable = false;
             response.Message = $"Lab has {conflictList.Count} conflicting reservation(s) during the requested time";
   response.ConflictingReservations = conflictList.Select(c => new ReservationDto
  {
        Id = c.Id,
     LabId = c.LabId,
      LabName = lab.Name,
   CustomerName = c.CustomerName,
        CustomerEmail = c.CustomerEmail,
  StartTime = c.StartTime,
      EndTime = c.EndTime,
          Purpose = c.Purpose,
      Status = c.Status,
            CreatedAt = c.CreatedAt,
          UpdatedAt = c.UpdatedAt
    }).ToList();
   return response;
   }

  _logger.LogInformation("Lab {LabId} is available for the requested time slot", labId);
       response.IsAvailable = true;
       response.Message = "Lab is available for the requested time slot";
            return response;
        }

        /// <summary>
        /// Get reservation summary/statistics for a specific lab
        /// Business logic: aggregates reservation data and calculates utilization metrics
        /// </summary>
        public async Task<ReservationSummaryResponse> GetLabReservationSummaryAsync(string labId)
      {
            _logger.LogInformation("Getting reservation summary for lab {LabId}", labId);

        var response = new ReservationSummaryResponse
            {
        LabId = labId
  };

  // Get the lab
      var lab = await _labRepository.GetByIdAsync(labId);
          if (lab == null)
      {
            _logger.LogWarning("Lab {LabId} not found for summary", labId);
   throw new InvalidOperationException($"Lab with ID {labId} not found");
  }

  response.LabName = lab.Name;

   // Get reservation counts by status
       response.TotalReservations = await _reservationRepository.GetCountByLabIdAsync(labId);
      response.PendingReservations = await _reservationRepository.GetCountByStatusAsync(labId, ReservationStatus.Pending);
      response.ConfirmedReservations = await _reservationRepository.GetCountByStatusAsync(labId, ReservationStatus.Confirmed);
     response.CancelledReservations = await _reservationRepository.GetCountByStatusAsync(labId, ReservationStatus.Cancelled);
      response.CompletedReservations = await _reservationRepository.GetCountByStatusAsync(labId, ReservationStatus.Completed);

 // Calculate utilization percentage (completed + confirmed out of non-cancelled)
          var activeReservations = response.CompletedReservations + response.ConfirmedReservations + response.PendingReservations;
  response.UtilizationPercentage = response.TotalReservations > 0 
     ? Math.Round((double)activeReservations / response.TotalReservations * 100, 2)
        : 0;

            _logger.LogInformation("Summary for lab {LabId}: Total={Total}, Utilization={Util}%", 
 labId, response.TotalReservations, response.UtilizationPercentage);

 return response;
        }
    }
}

using LabReservation.BL.Services.Interfaces;
using LabReservation.Models.Requests;
using LabReservation.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LabReservation.Host.Controllers
{
    /// <summary>
    /// CRUD Controller for Reservation operations
    /// </summary>
 [ApiController]
  [Route("api/[controller]")]
    [Produces("application/json")]
    public class ReservationsController : ControllerBase
{
        private readonly IReservationService _reservationService;
   private readonly ILogger<ReservationsController> _logger;

public ReservationsController(IReservationService reservationService, ILogger<ReservationsController> logger)
        {
     _reservationService = reservationService;
     _logger = logger;
     }

   /// <summary>
    /// Get all reservations
     /// </summary>
   [HttpGet]
     [ProducesResponseType(typeof(ReservationListResponse), StatusCodes.Status200OK)]
   public async Task<ActionResult<ReservationListResponse>> GetAll()
   {
     _logger.LogInformation("GET /api/reservations - Getting all reservations");
    var reservations = await _reservationService.GetAllAsync();
   var list = reservations.ToList();
       
 return Ok(new ReservationListResponse
   {
  Reservations = list,
   TotalCount = list.Count
         });
        }

        /// <summary>
  /// Get reservations by lab ID
        /// </summary>
    [HttpGet("lab/{labId}")]
        [ProducesResponseType(typeof(ReservationListResponse), StatusCodes.Status200OK)]
 public async Task<ActionResult<ReservationListResponse>> GetByLabId(string labId)
 {
            _logger.LogInformation("GET /api/reservations/lab/{LabId} - Getting reservations", labId);
var reservations = await _reservationService.GetByLabIdAsync(labId);
     var list = reservations.ToList();
      
  return Ok(new ReservationListResponse
 {
   Reservations = list,
     TotalCount = list.Count
});
  }

      /// <summary>
        /// Get reservations by customer email
      /// </summary>
        [HttpGet("customer/{email}")]
        [ProducesResponseType(typeof(ReservationListResponse), StatusCodes.Status200OK)]
     public async Task<ActionResult<ReservationListResponse>> GetByCustomerEmail(string email)
    {
     _logger.LogInformation("GET /api/reservations/customer/{Email} - Getting reservations", email);
     var reservations = await _reservationService.GetByCustomerEmailAsync(email);
      var list = reservations.ToList();
       
   return Ok(new ReservationListResponse
  {
Reservations = list,
  TotalCount = list.Count
 });
 }

  /// <summary>
    /// Get reservation by ID
        /// </summary>
     [HttpGet("{id}")]
     [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<ActionResult<ReservationResponse>> GetById(string id)
   {
     _logger.LogInformation("GET /api/reservations/{Id} - Getting reservation", id);
     var reservation = await _reservationService.GetByIdAsync(id);
          
    if (reservation == null)
         {
     return NotFound(new { message = $"Reservation with ID {id} not found" });
          }

  return Ok(new ReservationResponse { Reservation = reservation });
 }

    /// <summary>
   /// Create a new reservation
    /// </summary>
     [HttpPost]
       [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<ActionResult<ReservationResponse>> Create([FromBody] CreateReservationRequest request)
     {
   _logger.LogInformation("POST /api/reservations - Creating reservation for lab {LabId}", request.LabId);
          
try
     {
       var reservation = await _reservationService.CreateAsync(request);
      return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, 
 new ReservationResponse { Reservation = reservation });
      }
    catch (InvalidOperationException ex)
   {
        _logger.LogWarning("Failed to create reservation: {Message}", ex.Message);
    return BadRequest(new { message = ex.Message });
    }
        }

   /// <summary>
        /// Update an existing reservation
        /// </summary>
     [HttpPut("{id}")]
   [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReservationResponse>> Update(string id, [FromBody] UpdateReservationRequest request)
        {
  _logger.LogInformation("PUT /api/reservations/{Id} - Updating reservation", id);
     
  try
            {
           var reservation = await _reservationService.UpdateAsync(id, request);
      
  if (reservation == null)
   {
        return NotFound(new { message = $"Reservation with ID {id} not found" });
        }

      return Ok(new ReservationResponse { Reservation = reservation });
 }
 catch (InvalidOperationException ex)
 {
         _logger.LogWarning("Failed to update reservation: {Message}", ex.Message);
       return BadRequest(new { message = ex.Message });
   }
        }

   /// <summary>
        /// Delete a reservation
        /// </summary>
      [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
      {
       _logger.LogInformation("DELETE /api/reservations/{Id} - Deleting reservation", id);
 
     var result = await _reservationService.DeleteAsync(id);
     
     if (!result)
  {
           return NotFound(new { message = $"Reservation with ID {id} not found" });
}

            return NoContent();
   }

    /// <summary>
        /// Cancel a reservation
        /// </summary>
   [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Cancel(string id)
{
   _logger.LogInformation("POST /api/reservations/{Id}/cancel - Cancelling reservation", id);
        
       var result = await _reservationService.CancelReservationAsync(id);
        
   if (!result)
   {
  return NotFound(new { message = $"Reservation with ID {id} not found" });
 }

 return Ok(new { message = "Reservation cancelled successfully" });
        }

        /// <summary>
    /// Confirm a reservation
 /// </summary>
        [HttpPost("{id}/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> Confirm(string id)
  {
 _logger.LogInformation("POST /api/reservations/{Id}/confirm - Confirming reservation", id);
   
        var result = await _reservationService.ConfirmReservationAsync(id);
    
           if (!result)
            {
     return NotFound(new { message = $"Reservation with ID {id} not found" });
    }

  return Ok(new { message = "Reservation confirmed successfully" });
        }
  }
}

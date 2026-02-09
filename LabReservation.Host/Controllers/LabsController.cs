using LabReservation.BL.Services.Interfaces;
using LabReservation.Models.Requests;
using LabReservation.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LabReservation.Host.Controllers
{
    /// <summary>
    /// CRUD Controller for Lab operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LabsController : ControllerBase
    {
        private readonly ILabService _labService;
        private readonly ILogger<LabsController> _logger;

        public LabsController(ILabService labService, ILogger<LabsController> logger)
{
         _labService = labService;
            _logger = logger;
        }

        /// <summary>
        /// Get all labs
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(LabListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<LabListResponse>> GetAll()
        {
      _logger.LogInformation("GET /api/labs - Getting all labs");
     var labs = await _labService.GetAllAsync();
            var labList = labs.ToList();
  
     return Ok(new LabListResponse
            {
Labs = labList,
    TotalCount = labList.Count
  });
    }

        /// <summary>
   /// Get available labs only
  /// </summary>
        [HttpGet("available")]
 [ProducesResponseType(typeof(LabListResponse), StatusCodes.Status200OK)]
      public async Task<ActionResult<LabListResponse>> GetAvailable()
    {
    _logger.LogInformation("GET /api/labs/available - Getting available labs");
      var labs = await _labService.GetAvailableLabsAsync();
  var labList = labs.ToList();
  
   return Ok(new LabListResponse
       {
     Labs = labList,
       TotalCount = labList.Count
    });
        }

 /// <summary>
        /// Get lab by ID
        /// </summary>
     [HttpGet("{id}")]
     [ProducesResponseType(typeof(LabResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LabResponse>> GetById(string id)
        {
   _logger.LogInformation("GET /api/labs/{Id} - Getting lab", id);
     var lab = await _labService.GetByIdAsync(id);
    
 if (lab == null)
  {
           return NotFound(new { message = $"Lab with ID {id} not found" });
 }

         return Ok(new LabResponse { Lab = lab });
        }

        /// <summary>
        /// Create a new lab
     /// </summary>
        [HttpPost]
[ProducesResponseType(typeof(LabResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<ActionResult<LabResponse>> Create([FromBody] AddLabRequest request)
     {
       _logger.LogInformation("POST /api/labs - Creating new lab: {Name}", request.Name);
         
       var lab = await _labService.CreateAsync(request);
      return CreatedAtAction(nameof(GetById), new { id = lab.Id }, new LabResponse { Lab = lab });
     }

    /// <summary>
        /// Update an existing lab
        /// </summary>
        [HttpPut("{id}")]
      [ProducesResponseType(typeof(LabResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LabResponse>> Update(string id, [FromBody] UpdateLabRequest request)
 {
      _logger.LogInformation("PUT /api/labs/{Id} - Updating lab", id);
          
      var lab = await _labService.UpdateAsync(id, request);
    
     if (lab == null)
 {
        return NotFound(new { message = $"Lab with ID {id} not found" });
 }

     return Ok(new LabResponse { Lab = lab });
}

        /// <summary>
     /// Delete a lab
   /// </summary>
[HttpDelete("{id}")]
 [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
     {
            _logger.LogInformation("DELETE /api/labs/{Id} - Deleting lab", id);
     
    var result = await _labService.DeleteAsync(id);
         
if (!result)
  {
      return NotFound(new { message = $"Lab with ID {id} not found" });
 }

 return NoContent();
        }
    }
}

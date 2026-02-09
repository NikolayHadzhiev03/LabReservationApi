using LabReservation.Models.Entities;

namespace LabReservation.Models.DTO
{
    public class ReservationDto
    {
        public string Id { get; set; } = string.Empty;
      public string LabId { get; set; } = string.Empty;
        public string LabName { get; set; } = string.Empty;
public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
     public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
      public string Purpose { get; set; } = string.Empty;
   public ReservationStatus Status { get; set; }
 public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

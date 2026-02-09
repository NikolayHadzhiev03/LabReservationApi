using LabReservation.Models.Entities;

namespace LabReservation.Models.Requests
{
    public class UpdateReservationRequest
    {
        public DateTime StartTime { get; set; }
  public DateTime EndTime { get; set; }
    public string Purpose { get; set; } = string.Empty;
        public ReservationStatus Status { get; set; }
  }
}

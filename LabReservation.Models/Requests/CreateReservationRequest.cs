namespace LabReservation.Models.Requests
{
 public class CreateReservationRequest
  {
      public string LabId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }
}

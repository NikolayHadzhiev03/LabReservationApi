using LabReservation.Models.DTO;

namespace LabReservation.Models.Responses
{
 /// <summary>
    /// Response for lab availability check
    /// </summary>
    public class LabAvailabilityResponse
    {
        public string LabId { get; set; } = string.Empty;
        public string LabName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
      public List<ReservationDto> ConflictingReservations { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

  /// <summary>
    /// Response for reservation summary
    /// </summary>
    public class ReservationSummaryResponse
    {
    public string LabId { get; set; } = string.Empty;
       public string LabName { get; set; } = string.Empty;
        public int TotalReservations { get; set; }
        public int PendingReservations { get; set; }
     public int ConfirmedReservations { get; set; }
  public int CancelledReservations { get; set; }
        public int CompletedReservations { get; set; }
        public double UtilizationPercentage { get; set; }
  }
}

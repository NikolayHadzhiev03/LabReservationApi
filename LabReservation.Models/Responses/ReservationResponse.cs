using LabReservation.Models.DTO;

namespace LabReservation.Models.Responses
{
 public class ReservationResponse
    {
public ReservationDto Reservation { get; set; } = null!;
    }

  public class ReservationListResponse
    {
  public List<ReservationDto> Reservations { get; set; } = new();
        public int TotalCount { get; set; }
    }
}

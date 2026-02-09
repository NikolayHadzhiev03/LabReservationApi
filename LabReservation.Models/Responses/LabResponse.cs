using LabReservation.Models.DTO;

namespace LabReservation.Models.Responses
{
    public class LabResponse
    {
 public LabDto Lab { get; set; } = null!;
    }

    public class LabListResponse
    {
    public List<LabDto> Labs { get; set; } = new();
        public int TotalCount { get; set; }
    }
}

namespace LabReservation.Models.DTO
{
    public class LabDto
    {
        public string Id { get; set; } = string.Empty;
      public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public List<string> Equipment { get; set; } = new();
        public bool IsAvailable { get; set; }
      public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

namespace LabReservation.Models.Requests
{
    public class UpdateLabRequest
    {
     public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
        public List<string> Equipment { get; set; } = new();
        public bool IsAvailable { get; set; }
    }
}

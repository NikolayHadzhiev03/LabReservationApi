namespace LabReservation.Models.Requests
{
    public class AddLabRequest
    {
     public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
    public List<string> Equipment { get; set; } = new();
    }
}

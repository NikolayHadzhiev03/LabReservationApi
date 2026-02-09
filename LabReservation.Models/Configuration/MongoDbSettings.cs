namespace LabReservation.Models.Configuration
{
 /// <summary>
    /// MongoDB connection settings for IOptionsMonitor pattern
    /// </summary>
    public class MongoDbSettings
  {
      public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string LabsCollectionName { get; set; } = "labs";
        public string ReservationsCollectionName { get; set; } = "reservations";
    }
}

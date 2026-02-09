using LabReservation.DataLayer.Repositories.Interfaces;
using LabReservation.Models.Configuration;
using LabReservation.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LabReservation.DataLayer.Repositories.Implementations
{
    /// <summary>
  /// MongoDB implementation of Reservation repository
    /// </summary>
    public class ReservationRepository : IReservationRepository
    {
    private readonly IMongoCollection<Reservation> _reservationsCollection;

     public ReservationRepository(IOptionsMonitor<MongoDbSettings> settings)
        {
    var mongoClient = new MongoClient(settings.CurrentValue.ConnectionString);
        var database = mongoClient.GetDatabase(settings.CurrentValue.DatabaseName);
            _reservationsCollection = database.GetCollection<Reservation>(settings.CurrentValue.ReservationsCollectionName);
        }

    public async Task<Reservation?> GetByIdAsync(string id)
        {
        return await _reservationsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Reservation>> GetAllAsync()
    {
          return await _reservationsCollection.Find(_ => true).ToListAsync();
        }

     public async Task<IEnumerable<Reservation>> GetByLabIdAsync(string labId)
      {
            return await _reservationsCollection.Find(x => x.LabId == labId).ToListAsync();
    }

        public async Task<IEnumerable<Reservation>> GetByCustomerEmailAsync(string email)
{
       return await _reservationsCollection.Find(x => x.CustomerEmail == email).ToListAsync();
  }

public async Task<IEnumerable<Reservation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
 {
       return await _reservationsCollection
         .Find(x => x.StartTime >= startDate && x.EndTime <= endDate)
    .ToListAsync();
        }

      public async Task<IEnumerable<Reservation>> GetConflictingReservationsAsync(
            string labId, 
    DateTime startTime, 
       DateTime endTime, 
          string? excludeReservationId = null)
 {
   var filter = Builders<Reservation>.Filter.And(
        Builders<Reservation>.Filter.Eq(x => x.LabId, labId),
     Builders<Reservation>.Filter.Ne(x => x.Status, ReservationStatus.Cancelled),
      Builders<Reservation>.Filter.Or(
 Builders<Reservation>.Filter.And(
       Builders<Reservation>.Filter.Lte(x => x.StartTime, startTime),
        Builders<Reservation>.Filter.Gt(x => x.EndTime, startTime)
             ),
  Builders<Reservation>.Filter.And(
   Builders<Reservation>.Filter.Lt(x => x.StartTime, endTime),
    Builders<Reservation>.Filter.Gte(x => x.EndTime, endTime)
       ),
       Builders<Reservation>.Filter.And(
 Builders<Reservation>.Filter.Gte(x => x.StartTime, startTime),
       Builders<Reservation>.Filter.Lte(x => x.EndTime, endTime)
                    )
   )
   );

            if (!string.IsNullOrEmpty(excludeReservationId))
  {
     filter = Builders<Reservation>.Filter.And(
    filter,
    Builders<Reservation>.Filter.Ne(x => x.Id, excludeReservationId)
           );
      }

   return await _reservationsCollection.Find(filter).ToListAsync();
   }

        public async Task<Reservation> CreateAsync(Reservation reservation)
        {
            reservation.CreatedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;
await _reservationsCollection.InsertOneAsync(reservation);
    return reservation;
        }

        public async Task<Reservation?> UpdateAsync(string id, Reservation reservation)
        {
            reservation.UpdatedAt = DateTime.UtcNow;
    var result = await _reservationsCollection.ReplaceOneAsync(x => x.Id == id, reservation);
  return result.ModifiedCount > 0 ? reservation : null;
 }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _reservationsCollection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> ExistsAsync(string id)
        {
        return await _reservationsCollection.Find(x => x.Id == id).AnyAsync();
     }

     public async Task<int> GetCountByLabIdAsync(string labId)
        {
        return (int)await _reservationsCollection.CountDocumentsAsync(x => x.LabId == labId);
        }

      public async Task<int> GetCountByStatusAsync(string labId, ReservationStatus status)
        {
    return (int)await _reservationsCollection.CountDocumentsAsync(
         x => x.LabId == labId && x.Status == status);
  }
    }
}

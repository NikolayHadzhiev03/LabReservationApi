using LabReservation.DataLayer.Repositories.Interfaces;
using LabReservation.Models.Configuration;
using LabReservation.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LabReservation.DataLayer.Repositories.Implementations
{
    /// <summary>
    /// MongoDB implementation of Lab repository
    /// </summary>
    public class LabRepository : ILabRepository
{
   private readonly IMongoCollection<Lab> _labsCollection;

        public LabRepository(IOptionsMonitor<MongoDbSettings> settings)
      {
            var mongoClient = new MongoClient(settings.CurrentValue.ConnectionString);
          var database = mongoClient.GetDatabase(settings.CurrentValue.DatabaseName);
            _labsCollection = database.GetCollection<Lab>(settings.CurrentValue.LabsCollectionName);
}

        public async Task<Lab?> GetByIdAsync(string id)
        {
    return await _labsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Lab>> GetAllAsync()
        {
 return await _labsCollection.Find(_ => true).ToListAsync();
 }

        public async Task<IEnumerable<Lab>> GetAvailableLabsAsync()
     {
  return await _labsCollection.Find(x => x.IsAvailable).ToListAsync();
      }

 public async Task<Lab> CreateAsync(Lab lab)
 {
      lab.CreatedAt = DateTime.UtcNow;
     lab.UpdatedAt = DateTime.UtcNow;
      await _labsCollection.InsertOneAsync(lab);
      return lab;
        }

      public async Task<Lab?> UpdateAsync(string id, Lab lab)
        {
            lab.UpdatedAt = DateTime.UtcNow;
     var result = await _labsCollection.ReplaceOneAsync(x => x.Id == id, lab);
       return result.ModifiedCount > 0 ? lab : null;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _labsCollection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> ExistsAsync(string id)
 {
     return await _labsCollection.Find(x => x.Id == id).AnyAsync();
        }
    }
}

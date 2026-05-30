using LabReservation.BL.Services.Interfaces;
using LabReservation.BL.Services.Kafka;
using LabReservation.BL.Services.KafkaCache;
using LabReservation.DataLayer.Repositories.Interfaces;
using LabReservation.Models.DTO;
using LabReservation.Models.Entities;
using LabReservation.Models.Events;
using LabReservation.Models.Requests;
using Mapster;
using Microsoft.Extensions.Logging;

namespace LabReservation.BL.Services.Implementations
{
    /// <summary>
    /// Implementation of Lab service with CRUD operations
    /// </summary>
    public class LabService : ILabService
    {
        private readonly ILabRepository _labRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ICacheStore<Lab> _labCache;
        private readonly ILogger<LabService> _logger;

        public LabService(
            ILabRepository labRepository,
            IKafkaProducer kafkaProducer,
            ICacheStore<Lab> labCache,
            ILogger<LabService> logger)
        {
            _labRepository = labRepository;
            _kafkaProducer = kafkaProducer;
            _labCache = labCache;
            _logger = logger;
        }

        public async Task<LabDto?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Getting lab with ID: {LabId}", id);
            var lab = await _labRepository.GetByIdAsync(id);

            if (lab == null)
            {
                _logger.LogWarning("Lab with ID {LabId} not found", id);
                return null;
            }

            return lab.Adapt<LabDto>();
        }

        public async Task<IEnumerable<LabDto>> GetAllAsync()
        {
            // Serve from the in-memory cache (populated from Kafka snapshots) when available;
            // fall back to the database when the cache has not been hydrated yet.
            if (_labCache.Count > 0)
            {
                _logger.LogInformation("Getting all labs from cache ({Count} entries)", _labCache.Count);
                return _labCache.GetAll().Adapt<IEnumerable<LabDto>>();
            }

            _logger.LogInformation("Getting all labs from database (cache empty)");
            var labs = await _labRepository.GetAllAsync();
            return labs.Adapt<IEnumerable<LabDto>>();
        }

        public async Task<IEnumerable<LabDto>> GetAvailableLabsAsync()
        {
            if (_labCache.Count > 0)
            {
                _logger.LogInformation("Getting available labs from cache");
                var availableFromCache = _labCache.GetAll().Where(l => l.IsAvailable);
                return availableFromCache.Adapt<IEnumerable<LabDto>>();
            }

            _logger.LogInformation("Getting available labs from database (cache empty)");
            var labs = await _labRepository.GetAvailableLabsAsync();
            return labs.Adapt<IEnumerable<LabDto>>();
        }

        public async Task<LabDto> CreateAsync(AddLabRequest request)
        {
            _logger.LogInformation("Creating new lab: {LabName}", request.Name);

            var lab = request.Adapt<Lab>();
            lab.IsAvailable = true;

            var createdLab = await _labRepository.CreateAsync(lab);
            _logger.LogInformation("Lab created successfully with ID: {LabId}", createdLab.Id);

            await PublishEventSafelyAsync(KafkaEventTypes.LabCreated, new LabCreatedEvent(
                createdLab.Id,
                createdLab.Name,
                createdLab.Location,
                createdLab.Capacity,
                createdLab.Equipment.ToList(),
                createdLab.IsAvailable,
                createdLab.CreatedAt));

            return createdLab.Adapt<LabDto>();
        }

        public async Task<LabDto?> UpdateAsync(string id, UpdateLabRequest request)
        {
            _logger.LogInformation("Updating lab with ID: {LabId}", id);

            var existingLab = await _labRepository.GetByIdAsync(id);
            if (existingLab == null)
            {
                _logger.LogWarning("Lab with ID {LabId} not found for update", id);
                return null;
            }

            existingLab.Name = request.Name;
            existingLab.Location = request.Location;
            existingLab.Capacity = request.Capacity;
            existingLab.Equipment = request.Equipment;
            existingLab.IsAvailable = request.IsAvailable;

            var updatedLab = await _labRepository.UpdateAsync(id, existingLab);

            if (updatedLab == null)
            {
                _logger.LogError("Failed to update lab with ID: {LabId}", id);
                return null;
            }

            _logger.LogInformation("Lab updated successfully: {LabId}", id);

            await PublishEventSafelyAsync(KafkaEventTypes.LabUpdated, new LabUpdatedEvent(
                updatedLab.Id,
                updatedLab.Name,
                updatedLab.Location,
                updatedLab.Capacity,
                updatedLab.Equipment.ToList(),
                updatedLab.IsAvailable,
                updatedLab.UpdatedAt));

            return updatedLab.Adapt<LabDto>();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            _logger.LogInformation("Deleting lab with ID: {LabId}", id);
            var result = await _labRepository.DeleteAsync(id);

            if (result)
            {
                _logger.LogInformation("Lab deleted successfully: {LabId}", id);
                await PublishEventSafelyAsync(KafkaEventTypes.LabDeleted, new LabDeletedEvent(
                    id,
                    DateTime.UtcNow));
            }
            else
            {
                _logger.LogWarning("Lab with ID {LabId} not found for deletion", id);
            }

            return result;
        }

        // Publish failures must not break the API response (downstream broker outage shouldn't fail requests).
        // If at-least-once delivery from the API is ever required, replace with an outbox-pattern publish.
        private async Task PublishEventSafelyAsync<TPayload>(string eventType, TPayload payload)
            where TPayload : class
        {
            try
            {
                await _kafkaProducer.PublishAsync(eventType, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish Kafka event {EventType}", eventType);
            }
        }
    }
}

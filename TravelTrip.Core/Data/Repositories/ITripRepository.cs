using TravelTrip.Core.Models;

namespace TravelTrip.Core.Data.Repositories;

public interface ITripRepository
{
    Task<List<TripEntity>> GetTripsToVectorizeAsync(int batchSize);
    Task<List<TripEntity>> GetAllTripsAsync();
    Task<TripEntity?> GetTripByIdAsync(int id);
    Task<List<(TripEntity Entity, double Distance)>> SearchSimilarTripsAsync(Pgvector.Vector queryVector, int limit);
    Task<bool> UpdateTripAsync(TripEntity trip);
    Task<bool> SaveChangesAsync();
    Task ClearAllEmbeddingsAsync();
    Task AddSearchRankingLogAsync(SearchRankingLogEntity log);
}

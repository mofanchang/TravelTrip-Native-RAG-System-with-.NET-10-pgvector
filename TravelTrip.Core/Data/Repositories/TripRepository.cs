using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using TravelTrip.Core.Models;

namespace TravelTrip.Core.Data.Repositories;


public class TripRepository : ITripRepository
{
    private readonly TravelDbContext _context;

    public TripRepository(TravelDbContext context)
    {
        _context = context;
    }

    public async Task<List<TripEntity>> GetTripsToVectorizeAsync(int batchSize)
    {
        return await _context.Trips
            .Where(t => t.Embedding == null)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<List<TripEntity>> GetAllTripsAsync()
    {
        return await _context.Trips.ToListAsync();
    }

    public async Task<TripEntity?> GetTripByIdAsync(int id)
    {
        return await _context.Trips.FindAsync(id);
    }

    public async Task<List<(TripEntity Entity, double Distance)>> SearchSimilarTripsAsync(Pgvector.Vector queryVector, int limit)
    {
        var results = await _context.Trips
            .Select(t => new 
            { 
                Entity = t, 
                Distance = (double)t.Embedding!.CosineDistance(queryVector) 
            })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => (r.Entity, r.Distance)).ToList();
    }

    public async Task<bool> UpdateTripAsync(TripEntity trip)
    {
        _context.Trips.Update(trip);
        return await Task.FromResult(true);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task ClearAllEmbeddingsAsync()
    {
        var trips = await _context.Trips.ToListAsync();
        foreach (var trip in trips)
        {
            trip.Embedding = null;
        }
    }

    public async Task AddSearchRankingLogAsync(SearchRankingLogEntity log)
    {
        await _context.SearchRankingLogs.AddAsync(log);
    }
}

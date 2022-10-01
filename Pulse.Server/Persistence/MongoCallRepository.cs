using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Pulse.Server.Contracts;

namespace Pulse.Server.Persistence;

public class MongoCallRepository
{
    private readonly IMongoCollection<Call> calls;
    
    public MongoCallRepository(MongoClient mongoClient)
    {
        calls = mongoClient.GetDatabase(nameof(Pulse)).GetCollection<Call>("calls");
    }

    public async Task SaveAsync(Call call, CancellationToken cancellationToken = default)
    {
        await calls.ReplaceOneAsync(c => c.Id == call.Id, call, new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<Call> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await calls.AsQueryable().SingleAsync(call => call.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Call>> GetRecentCallsAsync(string username,
        CancellationToken cancellationToken = default)
    {
        var recentCalls = await calls.AsQueryable()
            .Where(c => c.Callee == username || c.Caller == username)
            .OrderByDescending(c => c.CallTime)
            .Take(20)
            .ToListAsync(cancellationToken);

        return recentCalls.AsReadOnly();
    }
}
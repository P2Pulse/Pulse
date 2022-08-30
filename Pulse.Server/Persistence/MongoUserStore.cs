using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace Pulse.Server.Persistence;

public class MongoUserStore : IUserStore<IdentityUser>, IUserPasswordStore<IdentityUser>
{
    private readonly IMongoCollection<IdentityUser> users;
    
    public MongoUserStore(MongoClient mongoClient)
    {
        // TODO: Create a UNIQUE index on the username field
        users = mongoClient.GetDatabase(nameof(Pulse))
            .GetCollection<IdentityUser>("users");
    }
    
    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public async Task SetUserNameAsync(IdentityUser user, string userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        await users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);
    }

    public Task<string> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public async Task SetNormalizedUserNameAsync(IdentityUser user, string normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        await users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);
    }

    public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await users.InsertOneAsync(user, cancellationToken: cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await users.DeleteOneAsync(u => u.Id == user.Id, cancellationToken: cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await users.Find(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IdentityUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return await users.Find(u => u.NormalizedUserName == normalizedUserName).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SetPasswordHashAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        await users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken);
    }

    public Task<string> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash is not null);
    }
}
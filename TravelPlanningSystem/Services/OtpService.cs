using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace TravelPlanningSystem.Services;

public sealed class OtpService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private const int MaximumAttempts = 5;
    private readonly IMemoryCache _cache;

    public OtpService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public OtpChallenge CreateChallenge(string email, string purpose)
    {
        var challengeId = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var challenge = new OtpChallenge(email, purpose, Hash(code), DateTimeOffset.UtcNow.Add(Lifetime));

        _cache.Set(CacheKey(challengeId), challenge, Lifetime);
        return new OtpChallenge(challengeId, email, purpose, code, challenge.ExpiresAt);
    }

    public bool Verify(string challengeId, string code)
    {
        if (!_cache.TryGetValue(CacheKey(challengeId), out OtpChallenge? challenge) || challenge is null)
            return false;

        if (challenge.ExpiresAt <= DateTimeOffset.UtcNow || challenge.Attempts >= MaximumAttempts)
        {
            _cache.Remove(CacheKey(challengeId));
            return false;
        }

        challenge.Attempts++;
        var suppliedHash = Hash(code.Trim());
        var valid = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(challenge.CodeHash),
            Convert.FromHexString(suppliedHash));

        if (valid)
            _cache.Remove(CacheKey(challengeId));

        return valid;
    }

    public void StorePending<T>(string challengeId, T value)
        => _cache.Set(PendingKey(challengeId), value, Lifetime);

    public bool TryGetPending<T>(string challengeId, out T? value)
        => _cache.TryGetValue(PendingKey(challengeId), out value);

    public void RemovePending(string challengeId)
        => _cache.Remove(PendingKey(challengeId));

    private static string CacheKey(string challengeId) => $"account-otp:{challengeId}";

    private static string PendingKey(string challengeId) => $"account-otp-pending:{challengeId}";

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed class OtpChallenge
{
    public OtpChallenge(string email, string purpose, string codeHash, DateTimeOffset expiresAt)
    {
        Email = email;
        Purpose = purpose;
        CodeHash = codeHash;
        ExpiresAt = expiresAt;
    }

    public OtpChallenge(string challengeId, string email, string purpose, string code, DateTimeOffset expiresAt)
        : this(email, purpose, string.Empty, expiresAt)
    {
        ChallengeId = challengeId;
        Code = code;
    }

    public string ChallengeId { get; }
    public string Email { get; }
    public string Purpose { get; }
    public string CodeHash { get; }
    public string? Code { get; }
    public DateTimeOffset ExpiresAt { get; }
    public int Attempts { get; set; }
}

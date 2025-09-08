using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace net_server;

public class MemorySessionStore : IDistributedCache
{
  private class CacheEntry
  {
    public byte[] Value { get; set; } = [];
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;
  }

  private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

  public byte[]? Get(string key)
  {
    if (_store.TryGetValue(key, out var entry))
    {
      if (IsExpired(entry))
      {
        _store.TryRemove(key, out _);
        return null;
      }

      UpdateLastAccess(entry);
      return entry.Value;
    }

    return null;
  }

  public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
  {
    return await Task.FromResult(Get(key));
  }

  public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
  {
    var entry = new CacheEntry
    {
      Value = value,
      AbsoluteExpiration = GetAbsoluteExpiration(options),
      SlidingExpiration = options.SlidingExpiration,
      LastAccessed = DateTimeOffset.UtcNow
    };

    _store[key] = entry;
  }

  public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
    CancellationToken token = default)
  {
    Set(key, value, options);
    await Task.CompletedTask;
  }

  public void Remove(string key)
  {
    _store.TryRemove(key, out _);
  }

  public async Task RemoveAsync(string key, CancellationToken token = default)
  {
    Remove(key);
    await Task.CompletedTask;
  }

  public void Refresh(string key)
  {
    if (_store.TryGetValue(key, out var entry))
    {
      if (!IsExpired(entry))
        UpdateLastAccess(entry);
    }
  }

  public async Task RefreshAsync(string key, CancellationToken token = default)
  {
    Refresh(key);
    await Task.CompletedTask;
  }

  // Helpers

  private static DateTimeOffset? GetAbsoluteExpiration(DistributedCacheEntryOptions options)
  {
    if (options.AbsoluteExpiration.HasValue)
      return options.AbsoluteExpiration.Value;

    if (options.AbsoluteExpirationRelativeToNow.HasValue)
      return DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);

    return null;
  }

  private static bool IsExpired(CacheEntry entry)
  {
    var now = DateTimeOffset.UtcNow;

    if (entry.AbsoluteExpiration.HasValue && entry.AbsoluteExpiration <= now)
      return true;

    if (entry.SlidingExpiration.HasValue &&
        entry.LastAccessed.Add(entry.SlidingExpiration.Value) <= now)
      return true;

    return false;
  }

  private static void UpdateLastAccess(CacheEntry entry)
  {
    entry.LastAccessed = DateTimeOffset.UtcNow;
  }
}


public class CookieStore : ITicketStore
{
  private readonly ConcurrentDictionary<string, AuthenticationTicket> _store = new();

  public Task<string> StoreAsync(AuthenticationTicket ticket)
  {
    //string key = Guid.NewGuid().ToString();
    if (ticket is not null)
    {
      var key = ticket.Principal.Claims.First(x => x.Type == ClaimTypes.Email).Value;

      _store.TryAdd(key, ticket);

      return Task.FromResult(key);
    }

    return Task.FromResult(string.Empty);
  }

  public Task RenewAsync(string key, AuthenticationTicket ticket)
  {
    _store.TryUpdate(key, ticket, ticket);
    return Task.CompletedTask;
  }

  public Task<AuthenticationTicket?> RetrieveAsync(string key)
  {
    if (key is null)
      return Task.FromResult<AuthenticationTicket?>(null);

    bool found = _store.TryGetValue(key, out var ticket);

    return found ? Task.FromResult(ticket) : Task.FromResult<AuthenticationTicket?>(null);
  }

  public async Task<bool> IsExpiredAsync(string key)
  {
    var ticket = await RetrieveAsync(key);

    if(ticket is null)
      return false;

    return ticket.Properties.ExpiresUtc < DateTimeOffset.UtcNow;
  }

  public Task RemoveAsync(string key)
  {
    _store.TryRemove(key, out _);

    return Task.CompletedTask;
  }
}

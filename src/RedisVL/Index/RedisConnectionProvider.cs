using StackExchange.Redis;

namespace RedisVL.Index;

/// <summary>
/// Provides and manages Redis connections.
/// </summary>
public class RedisConnectionProvider : IDisposable
{
    private readonly ConnectionMultiplexer _connection;
    private bool _disposed;
    
    /// <summary>
    /// Creates a connection provider from a Redis URL.
    /// </summary>
    /// <param name="redisUrl">Redis connection string (e.g., "redis://localhost:6379").</param>
    public RedisConnectionProvider(string redisUrl)
    {
        var connectionString = ParseRedisUrl(redisUrl);
        _connection = ConnectionMultiplexer.Connect(connectionString);
    }
    
    /// <summary>
    /// Creates a connection provider from an existing ConnectionMultiplexer.
    /// </summary>
    public RedisConnectionProvider(ConnectionMultiplexer connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    /// <summary>
    /// Creates a connection provider from ConfigurationOptions.
    /// </summary>
    public RedisConnectionProvider(ConfigurationOptions options)
    {
        _connection = ConnectionMultiplexer.Connect(options);
    }
    
    /// <summary>
    /// Gets the Redis database.
    /// </summary>
    public IDatabase GetDatabase(int db = -1) => _connection.GetDatabase(db);
    
    /// <summary>
    /// Gets the underlying ConnectionMultiplexer.
    /// </summary>
    public ConnectionMultiplexer Connection => _connection;
    
    /// <summary>
    /// Parses a Redis URL into a StackExchange.Redis connection string.
    /// Supports formats: redis://host:port, redis://password@host:port, host:port
    /// </summary>
    private static string ParseRedisUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("Redis URL cannot be null or empty.", nameof(url));
        
        // If it doesn't start with redis://, assume it's already a valid connection string
        if (!url.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }
        
        var isSsl = url.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
        var uri = new Uri(url.Replace("redis://", "http://").Replace("rediss://", "https://"));
        
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 6379;
        var password = string.IsNullOrEmpty(uri.UserInfo) ? null : Uri.UnescapeDataString(uri.UserInfo);
        
        // Handle user:password format
        if (password?.Contains(':') == true)
        {
            password = password.Split(':').Last();
        }
        
        var parts = new List<string> { $"{host}:{port}" };
        
        if (!string.IsNullOrEmpty(password))
            parts.Add($"password={password}");
        
        if (isSsl)
            parts.Add("ssl=true");
        
        // Extract database from path
        if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
        {
            var dbStr = uri.AbsolutePath.TrimStart('/');
            if (int.TryParse(dbStr, out var db))
                parts.Add($"defaultDatabase={db}");
        }
        
        return string.Join(",", parts);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

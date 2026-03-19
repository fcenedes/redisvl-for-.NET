using System.Net;
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
    /// Creates a connection provider for a Redis Sentinel setup.
    /// </summary>
    /// <param name="serviceName">The Sentinel service name (master name).</param>
    /// <param name="sentinelEndpoints">Sentinel endpoint addresses.</param>
    /// <param name="password">Optional password for the Redis master.</param>
    public RedisConnectionProvider(string serviceName, IEnumerable<EndPoint> sentinelEndpoints, string? password = null)
    {
        var options = new ConfigurationOptions
        {
            ServiceName = serviceName,
            CommandMap = CommandMap.Sentinel
        };

        foreach (var ep in sentinelEndpoints)
            options.EndPoints.Add(ep);

        if (!string.IsNullOrEmpty(password))
            options.Password = password;

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
    /// Supports formats: redis://host:port, redis://password@host:port, host:port,
    /// sentinel://host1:port1,host2:port2/serviceName
    /// </summary>
    internal static string ParseRedisUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("Redis URL cannot be null or empty.", nameof(url));

        // Handle sentinel:// URLs
        if (url.StartsWith("sentinel://", StringComparison.OrdinalIgnoreCase))
        {
            return ParseSentinelUrl(url);
        }

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

    /// <summary>
    /// Parses a sentinel:// URL into a StackExchange.Redis connection string.
    /// Format: sentinel://host1:port1,host2:port2/serviceName
    /// Optionally: sentinel://password@host1:port1,host2:port2/serviceName
    /// </summary>
    internal static string ParseSentinelUrl(string url)
    {
        // Remove scheme
        var body = url.Substring("sentinel://".Length);

        string? password = null;

        // Check for password (before @)
        var atIndex = body.IndexOf('@');
        if (atIndex >= 0)
        {
            password = Uri.UnescapeDataString(body.Substring(0, atIndex));
            body = body.Substring(atIndex + 1);
        }

        // Split service name from path (last segment after /)
        var slashIndex = body.IndexOf('/');
        if (slashIndex < 0)
            throw new ArgumentException("Sentinel URL must include a service name in the path (e.g., sentinel://host:port/mymaster).");

        var hostsPart = body.Substring(0, slashIndex);
        var serviceName = body.Substring(slashIndex + 1);

        if (string.IsNullOrEmpty(serviceName))
            throw new ArgumentException("Sentinel URL must include a non-empty service name.");

        var parts = new List<string>();

        // Parse comma-separated host:port pairs
        foreach (var hostPort in hostsPart.Split(','))
        {
            var trimmed = hostPort.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                parts.Add(trimmed);
        }

        if (parts.Count == 0)
            throw new ArgumentException("Sentinel URL must include at least one host:port.");

        parts.Add($"serviceName={serviceName}");

        if (!string.IsNullOrEmpty(password))
            parts.Add($"password={password}");

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

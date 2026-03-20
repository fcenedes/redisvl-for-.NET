# Connections

RedisVL supports multiple connection methods through `RedisConnectionProvider`.

## Connection URL Formats

### Standard Redis

```csharp
// Default localhost
var index = new SearchIndex(schema, "redis://localhost:6379");

// With password
var index = new SearchIndex(schema, "redis://mypassword@host:6379");

// With username and password
var index = new SearchIndex(schema, "redis://user:password@host:6379");

// With database number
var index = new SearchIndex(schema, "redis://localhost:6379/2");

// Full URL
var index = new SearchIndex(schema, "redis://user:password@host:6379/0");
```

### SSL/TLS (rediss://)

```csharp
var index = new SearchIndex(schema, "rediss://host:6380");
var index = new SearchIndex(schema, "rediss://user:password@host:6380/0");
```

### Redis Sentinel

Connect through Redis Sentinel for high-availability setups:

```csharp
// URL format
var index = new SearchIndex(schema, "sentinel://sentinel1:26379,sentinel2:26379,sentinel3:26379/mymaster");

// With password
var index = new SearchIndex(schema, "sentinel://password@sentinel1:26379,sentinel2:26379/mymaster");
```

### Plain Connection String

StackExchange.Redis connection strings are passed through directly:

```csharp
var index = new SearchIndex(schema, "myhost:6379,password=abc,ssl=true");
```

## Using RedisConnectionProvider Directly

For advanced scenarios, create a `RedisConnectionProvider` and share it:

```csharp
using RedisVL.Index;

// From URL
var provider = new RedisConnectionProvider("redis://localhost:6379");

// From existing ConnectionMultiplexer
var mux = ConnectionMultiplexer.Connect("localhost:6379");
var provider = new RedisConnectionProvider(mux);

// From ConfigurationOptions
var options = new ConfigurationOptions
{
    EndPoints = { "localhost:6379" },
    Password = "secret",
    Ssl = true
};
var provider = new RedisConnectionProvider(options);

// Sentinel constructor
var provider = new RedisConnectionProvider(
    serviceName: "mymaster",
    sentinelEndpoints: new EndPoint[]
    {
        new DnsEndPoint("sentinel1", 26379),
        new DnsEndPoint("sentinel2", 26379),
        new DnsEndPoint("sentinel3", 26379)
    },
    password: "optional-password"
);

// Access the underlying connection
IDatabase db = provider.GetDatabase();
ConnectionMultiplexer connection = provider.GetConnection();
```

## Connection Lifecycle

`SearchIndex` and extensions implement `IDisposable`. Always dispose when done:

```csharp
// Using statement (recommended)
using var index = new SearchIndex(schema, "redis://localhost:6379");
await index.CreateAsync();
// ... use index ...
// Automatically disposed at end of scope

// Manual disposal
var index = new SearchIndex(schema, "redis://localhost:6379");
try
{
    await index.CreateAsync();
    // ... use index ...
}
finally
{
    index.Dispose();
}
```

## URL Parsing Reference

| URL Format | Result |
|---|---|
| `redis://host:6379` | `host:6379` |
| `redis://pass@host:6379` | `host:6379,password=pass` |
| `redis://user:pass@host:6379` | `host:6379,password=pass` |
| `redis://host:6379/2` | `host:6379,defaultDatabase=2` |
| `rediss://host:6380` | `host:6380,ssl=true` |
| `sentinel://s1:26379,s2:26379/mymaster` | `s1:26379,s2:26379,serviceName=mymaster` |
| `host:6379,password=abc` | Passed through as-is |


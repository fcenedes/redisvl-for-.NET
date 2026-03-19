using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RedisVL.Index;
using RedisVL.Schema;
using StackExchange.Redis;

namespace RedisVL.Extensions.Cache;

/// <summary>
/// A cached embedding entry.
/// </summary>
public class EmbeddingsCacheEntry
{
    /// <summary>The original text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The model name used to generate the embedding.</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>The cached embedding vector.</summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>Optional metadata.</summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Cache for storing and retrieving embeddings by text and model name.
/// Avoids redundant embedding API calls by caching results in Redis.
/// </summary>
public class EmbeddingsCache : IDisposable
{
    private readonly RedisConnectionProvider _provider;
    private readonly IDatabase _db;
    private readonly string _prefix;
    private readonly TimeSpan? _defaultTtl;
    private bool _disposed;

    /// <summary>
    /// Creates an embeddings cache.
    /// </summary>
    /// <param name="redisUrl">Redis connection URL.</param>
    /// <param name="prefix">Key prefix for cached entries. Default: "embcache".</param>
    /// <param name="ttl">Default time-to-live for cache entries.</param>
    public EmbeddingsCache(
        string redisUrl = "redis://localhost:6379",
        string prefix = "embcache",
        TimeSpan? ttl = null)
    {
        _provider = new RedisConnectionProvider(redisUrl);
        _db = _provider.GetDatabase();
        _prefix = prefix;
        _defaultTtl = ttl;
    }

    /// <summary>
    /// Stores an embedding with text and metadata.
    /// </summary>
    public string Set(string text, string modelName, float[] embedding,
        Dictionary<string, string>? metadata = null, TimeSpan? ttl = null)
        => SetAsync(text, modelName, embedding, metadata, ttl).GetAwaiter().GetResult();

    /// <summary>
    /// Stores an embedding with text and metadata (async).
    /// </summary>
    public async Task<string> SetAsync(string text, string modelName, float[] embedding,
        Dictionary<string, string>? metadata = null, TimeSpan? ttl = null)
    {
        var key = BuildKey(text, modelName);
        var entries = new HashEntry[]
        {
            new("text", text),
            new("model_name", modelName),
            new("embedding", EmbeddingToBytes(embedding)),
            new("dims", embedding.Length)
        };

        var allEntries = metadata != null
            ? entries.Append(new HashEntry("metadata", JsonSerializer.Serialize(metadata))).ToArray()
            : entries;

        await _db.HashSetAsync(key, allEntries);

        var effectiveTtl = ttl ?? _defaultTtl;
        if (effectiveTtl.HasValue)
            await _db.KeyExpireAsync(key, effectiveTtl.Value);

        return key;
    }

    /// <summary>
    /// Retrieves a cached embedding by text and model name.
    /// </summary>
    public EmbeddingsCacheEntry? Get(string text, string modelName)
        => GetAsync(text, modelName).GetAwaiter().GetResult();

    /// <summary>
    /// Retrieves a cached embedding by text and model name (async).
    /// </summary>
    public async Task<EmbeddingsCacheEntry?> GetAsync(string text, string modelName)
    {
        var key = BuildKey(text, modelName);
        return await GetByKeyInternalAsync(key);
    }

    /// <summary>
    /// Retrieves a cached embedding by its Redis key.
    /// </summary>
    public EmbeddingsCacheEntry? GetByKey(string key)
        => GetByKeyAsync(key).GetAwaiter().GetResult();

    /// <summary>
    /// Retrieves a cached embedding by its Redis key (async).
    /// </summary>
    public async Task<EmbeddingsCacheEntry?> GetByKeyAsync(string key)
        => await GetByKeyInternalAsync(key);

    /// <summary>
    /// Checks if an embedding exists for the given text and model.
    /// </summary>
    public bool Exists(string text, string modelName)
        => ExistsAsync(text, modelName).GetAwaiter().GetResult();

    /// <summary>
    /// Checks if an embedding exists for the given text and model (async).
    /// </summary>
    public async Task<bool> ExistsAsync(string text, string modelName)
        => await _db.KeyExistsAsync(BuildKey(text, modelName));

    /// <summary>
    /// Checks if an embedding exists by its Redis key.
    /// </summary>
    public bool ExistsByKey(string key)
        => ExistsByKeyAsync(key).GetAwaiter().GetResult();

    /// <summary>
    /// Checks if an embedding exists by its Redis key (async).
    /// </summary>
    public async Task<bool> ExistsByKeyAsync(string key)
        => await _db.KeyExistsAsync(key);

    /// <summary>
    /// Removes a cached embedding by text and model name.
    /// </summary>
    public void Drop(string text, string modelName)
        => DropAsync(text, modelName).GetAwaiter().GetResult();



    /// <summary>
    /// Removes a cached embedding by text and model name (async).
    /// </summary>
    public async Task DropAsync(string text, string modelName)
        => await _db.KeyDeleteAsync(BuildKey(text, modelName));

    /// <summary>
    /// Removes a cached embedding by its Redis key.
    /// </summary>
    public void DropByKey(string key)
        => DropByKeyAsync(key).GetAwaiter().GetResult();

    /// <summary>
    /// Removes a cached embedding by its Redis key (async).
    /// </summary>
    public async Task DropByKeyAsync(string key)
        => await _db.KeyDeleteAsync(key);

    // ── Batch Operations ──

    /// <summary>
    /// Stores multiple embeddings.
    /// </summary>
    public IList<string> MSet(IList<string> texts, string modelName, IList<float[]> embeddings,
        IList<Dictionary<string, string>>? metadatas = null, TimeSpan? ttl = null)
        => MSetAsync(texts, modelName, embeddings, metadatas, ttl).GetAwaiter().GetResult();

    /// <summary>
    /// Stores multiple embeddings (async).
    /// </summary>
    public async Task<IList<string>> MSetAsync(IList<string> texts, string modelName, IList<float[]> embeddings,
        IList<Dictionary<string, string>>? metadatas = null, TimeSpan? ttl = null)
    {
        if (texts.Count != embeddings.Count)
            throw new ArgumentException("texts and embeddings must have the same count.");

        var keys = new List<string>(texts.Count);
        var tasks = new List<Task>();

        for (int i = 0; i < texts.Count; i++)
        {
            var meta = metadatas != null && i < metadatas.Count ? metadatas[i] : null;
            tasks.Add(SetAsync(texts[i], modelName, embeddings[i], meta, ttl));
        }

        // SetAsync returns keys so we need to capture them
        keys.Clear();
        for (int i = 0; i < texts.Count; i++)
            keys.Add(BuildKey(texts[i], modelName));

        await Task.WhenAll(tasks);
        return keys;
    }

    /// <summary>
    /// Retrieves multiple cached embeddings.
    /// </summary>
    public IList<EmbeddingsCacheEntry?> MGet(IList<string> texts, string modelName)
        => MGetAsync(texts, modelName).GetAwaiter().GetResult();

    /// <summary>
    /// Retrieves multiple cached embeddings (async).
    /// </summary>
    public async Task<IList<EmbeddingsCacheEntry?>> MGetAsync(IList<string> texts, string modelName)
    {
        var tasks = texts.Select(t => GetAsync(t, modelName)).ToArray();
        var results = await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Checks existence of multiple cached embeddings.
    /// </summary>
    public IList<bool> MExists(IList<string> texts, string modelName)
        => MExistsAsync(texts, modelName).GetAwaiter().GetResult();

    /// <summary>
    /// Checks existence of multiple cached embeddings (async).
    /// </summary>
    public async Task<IList<bool>> MExistsAsync(IList<string> texts, string modelName)
    {
        var tasks = texts.Select(t => ExistsAsync(t, modelName)).ToArray();
        var results = await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Removes multiple cached embeddings.
    /// </summary>
    public void MDrop(IList<string> texts, string modelName)
        => MDropAsync(texts, modelName).GetAwaiter().GetResult();

    /// <summary>
    /// Removes multiple cached embeddings (async).
    /// </summary>
    public async Task MDropAsync(IList<string> texts, string modelName)
    {
        var keys = texts.Select(t => (RedisKey)BuildKey(t, modelName)).ToArray();
        await _db.KeyDeleteAsync(keys);
    }

    /// <summary>
    /// Clears all entries with the cache prefix.
    /// </summary>
    public async Task ClearAsync()
    {
        var server = _provider.Connection.GetServers().First();
        var keys = server.Keys(pattern: $"{_prefix}:*").ToArray();
        if (keys.Length > 0)
            await _db.KeyDeleteAsync(keys);
    }

    // ── Private Helpers ──

    private string BuildKey(string text, string modelName)
    {
        var hash = ComputeHash($"{modelName}:{text}");
        return $"{_prefix}:{hash}";
    }

    private async Task<EmbeddingsCacheEntry?> GetByKeyInternalAsync(string key)
    {
        var entries = await _db.HashGetAllAsync(key);
        if (entries.Length == 0)
            return null;

        var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value);

        return new EmbeddingsCacheEntry
        {
            Text = dict.TryGetValue("text", out var t) ? t.ToString() : string.Empty,
            ModelName = dict.TryGetValue("model_name", out var m) ? m.ToString() : string.Empty,
            Embedding = dict.TryGetValue("embedding", out var emb) ? BytesToEmbedding((byte[])emb!) : Array.Empty<float>(),
            Metadata = dict.TryGetValue("metadata", out var meta) && meta.HasValue
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(meta.ToString())
                : null
        };
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static byte[] EmbeddingToBytes(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _provider.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
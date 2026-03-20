# Extensions

RedisVL provides high-level extensions built on top of the core search index for common AI/ML patterns.

## Semantic Cache

Cache LLM responses by semantic similarity to reduce API costs and latency.

```csharp
using RedisVL.Extensions.Cache;

var cache = new SemanticCache(
    indexName: "llm-cache",
    vectorizer: vectorizer,
    redisUrl: "redis://localhost:6379",
    distanceThreshold: 0.1    // similarity threshold (lower = stricter)
);

// Store a response
await cache.StoreAsync(
    prompt: "What is Redis?",
    response: "Redis is an in-memory data store...",
    metadata: new Dictionary<string, string> { ["model"] = "gpt-4" }
);

// Check for cached response
var hits = await cache.CheckAsync("Tell me about Redis");
if (hits.Count > 0)
{
    Console.WriteLine(hits[0].Response);       // cached response
    Console.WriteLine(hits[0].Metadata["model"]); // "gpt-4"
}

// Clear all cached entries
await cache.ClearAsync();
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `indexName` | `string` | — | Redis index name |
| `vectorizer` | `ITextVectorizer` | — | Vectorizer for embedding prompts |
| `redisUrl` | `string` | — | Redis connection URL |
| `distanceThreshold` | `double` | `0.1` | Max distance for cache hit |

## Embeddings Cache

Cache raw embeddings by text + model key to avoid redundant API calls.

```csharp
using RedisVL.Extensions.Cache;

var cache = new EmbeddingsCache(
    redisUrl: "redis://localhost:6379",
    prefix: "emb-cache"
);

// Single operations
await cache.SetAsync("hello world", "text-embedding-3-small", embedding);
var entry = await cache.GetAsync("hello world", "text-embedding-3-small");
bool exists = await cache.ExistsAsync("hello world", "text-embedding-3-small");
await cache.DropAsync("hello world", "text-embedding-3-small");

// Batch operations
await cache.MSetAsync(texts, "model-name", embeddings);
var entries = await cache.MGetAsync(texts, "model-name");
var existsFlags = await cache.MExistsAsync(texts, "model-name");
await cache.MDropAsync(texts, "model-name");

// With metadata
await cache.SetAsync("text", "model", embedding,
    metadata: new Dictionary<string, string> { ["source"] = "api" });

// Clear all
await cache.ClearAsync();
```

## Message History

### BaseMessageHistory

Store and retrieve LLM conversation history.

```csharp
using RedisVL.Extensions.MessageHistory;

var history = new BaseMessageHistory(
    sessionId: "user-session-123",
    redisUrl: "redis://localhost:6379"
);

// Add messages
await history.AddMessageAsync(new Message { Role = "user", Content = "Hello!" });
await history.AddMessageAsync(new Message
{
    Role = "llm",
    Content = "Hi there!",
    Metadata = new Dictionary<string, string> { ["model"] = "gpt-4" }
});

// Batch add
await history.AddMessagesAsync(new[]
{
    new Message { Role = "user", Content = "What is Redis?" },
    new Message { Role = "llm", Content = "Redis is..." }
});

// Get recent messages
var recent = await history.GetRecentAsync(topK: 10);

// Filter by single role
var userMessages = await history.GetRecentAsync(topK: 10, role: "user");

// Filter by multiple roles
var conversation = await history.GetRecentAsync(topK: 20, roles: new[] { "user", "llm" });

// Count and clear
var count = await history.CountAsync();
await history.ClearAsync();
```

### SemanticMessageHistory

Extends `BaseMessageHistory` with semantic search over past messages.

```csharp
var history = new SemanticMessageHistory(
    sessionId: "session-456",
    vectorizer: vectorizer,
    redisUrl: "redis://localhost:6379",
    distanceThreshold: 0.5
);

// Add messages (automatically embedded)
await history.AddMessageAsync(new Message { Role = "user", Content = "Tell me about Redis" });

// Semantic search over history
var relevant = await history.GetRelevantAsync("database performance", topK: 5);

// With role filter
var userRelevant = await history.GetRelevantAsync("database", topK: 5, role: "user");
```

## Semantic Router

Classify user queries into predefined routes using vector similarity.

```csharp
using RedisVL.Extensions.Router;

var routes = new List<Route>
{
    new Route
    {
        Name = "greeting",
        References = new List<string> { "hello", "hi", "hey", "good morning" },
        DistanceThreshold = 0.5
    },
    new Route
    {
        Name = "farewell",
        References = new List<string> { "bye", "goodbye", "see you later" },
        DistanceThreshold = 0.5
    },
    new Route
    {
        Name = "technical",
        References = new List<string> { "how do I", "what is the API", "code example" },
        DistanceThreshold = 0.7
    }
};

var router = new SemanticRouter(
    indexName: "my-router",
    routes: routes,
    vectorizer: vectorizer,
    redisUrl: "redis://localhost:6379"
);

// Route a query
var match = await router.RouteAsync("Hello there!");
if (match != null)
{
    Console.WriteLine($"Route: {match.Name}");      // "greeting"
    Console.WriteLine($"Distance: {match.Distance}"); // similarity score
}

// No match returns null
var noMatch = await router.RouteAsync("xyzzy gibberish");
// noMatch == null

// InvokeAsync is an alias for RouteAsync
var result = await router.InvokeAsync("goodbye!");

// Clean up
await router.ClearAsync();
```


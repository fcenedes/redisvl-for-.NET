# RedisVL for .NET

A .NET client library for [Redis](https://redis.io/) vector search and AI-powered extensions, inspired by [RedisVL for Python](https://github.com/redis/redis-vl-python).

## Features

- **Schema-driven index management** — Define index schemas in YAML or JSON, create/drop Redis search indexes
- **Vector similarity search** — KNN vector queries, range queries, and hybrid (vector + text) queries
- **Full-text search** — BM25 text search with field targeting and filters
- **Rich filter expressions** — Tag, Numeric, Geo, and Text filters with boolean combinators (`&`, `|`, `~`)
- **Semantic Cache** — Cache LLM responses by semantic similarity to reduce API costs
- **Message History** — Store and retrieve LLM conversation history with optional semantic search
- **Semantic Router** — Classify queries into predefined routes using vector similarity
- **Multiple vectorizers** — OpenAI, Azure OpenAI, Cohere, HuggingFace embedding integrations
- **Reranking** — Cohere reranker integration for result refinement

## Requirements

- **.NET 10** (net10.0 target framework)
- **Redis 8+** with RediSearch module (for vector search and full-text search)
- API keys for vectorizer/reranker providers (OpenAI, Cohere, HuggingFace, Azure OpenAI) as needed

## Installation

Add the project reference or install from NuGet (when published):

```bash
dotnet add reference src/RedisVL/RedisVL.csproj
```

## Quick Start

```csharp
using RedisVL.Schema;
using RedisVL.Index;
using RedisVL.Query;
using RedisVL.Query.Filter;

// 1. Define schema
var schema = IndexSchema.FromJson(@"{
    ""index"": { ""name"": ""products"", ""prefix"": ""product"", ""storage_type"": ""hash"" },
    ""fields"": [
        { ""name"": ""title"", ""type"": ""text"" },
        { ""name"": ""category"", ""type"": ""tag"" },
        { ""name"": ""price"", ""type"": ""numeric"" },
        { ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": {
            ""algorithm"": ""hnsw"", ""dims"": 1536, ""distance_metric"": ""cosine""
        }}
    ]
}");

// 2. Create index
var index = new SearchIndex(schema, "redis://localhost:6379");
await index.CreateAsync(overwrite: true);

// 3. Load data
await index.LoadAsync(new[] {
    new Dictionary<string, object> {
        ["title"] = "Laptop",
        ["category"] = "electronics",
        ["price"] = 999.99,
        ["embedding"] = new float[1536]
    }
});

// 4. Query with filters
var query = new VectorQuery(queryVector, "embedding", numResults: 5)
{
    FilterExpression = (Tag.Field("category") == "electronics") & (Num.Field("price") <= 1500)
};
var results = await index.QueryAsync(query);
```

## API Usage

### Schema Definition

```csharp
// From YAML
var schema = IndexSchema.FromYaml("schema.yaml");

// From JSON string
var schema = IndexSchema.FromJson(jsonString);
```

### Filters

```csharp
var tag = Tag.Field("category") == "electronics";
var numeric = Num.Field("price").Between(10, 100);
var geo = Geo.Field("location").WithinRadius(-73.9, 40.7, 10, GeoUnit.Kilometers);
var text = Text.Field("title").Match("laptop");
var combined = (tag & numeric) | text;
```

### Queries

```csharp
// Vector KNN search
var vq = new VectorQuery(vector, "embedding", numResults: 10);

// Range search
var rq = new RangeQuery(vector, "embedding", distanceThreshold: 0.5);

// Hybrid vector + text search
var hq = new HybridQuery(vector, "embedding", "search text", "content");

// Full-text search
var tq = new TextQuery("redis database", "content");

// Filter-only query
var fq = new FilterQuery(Tag.Field("status") == "active");

// Count query
var cq = new CountQuery(Num.Field("price") >= 50);
```

### Extensions

```csharp
// Semantic Cache
var cache = new SemanticCache("my-cache", vectorizer, distanceThreshold: 0.1);
await cache.StoreAsync("What is Redis?", "Redis is an in-memory database.");
var hits = await cache.CheckAsync("Tell me about Redis");

// Semantic Router
var router = new SemanticRouter("my-router", routes, vectorizer);
var match = await router.RouteAsync("Hello there!");

// Message History
var history = new SemanticMessageHistory("session-1", vectorizer);
await history.AddMessageAsync(new Message { Role = "user", Content = "Hi" });
var relevant = await history.GetRelevantAsync("greeting");
```

## Testing

Run all unit tests:

```bash
dotnet test
```

Run with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Coverage Gap Summary

| Area | Unit Testable | Integration Test Required |
|------|:---:|:---:|
| Schema parsing & validation | ✅ | — |
| Query string generation | ✅ | — |
| Filter expression building | ✅ | — |
| Field attribute parsing | ✅ | — |
| Exception construction | ✅ | — |
| Vectorizer/Reranker constructors | ✅ | — |
| SearchIndex CRUD (Create/Drop/Exists) | — | ✅ Redis required |
| SearchIndex Load/Fetch/Delete | — | ✅ Redis required |
| SearchIndex QueryAsync | — | ✅ Redis required |
| SemanticCache Store/Check | — | ✅ Redis + API required |
| SemanticMessageHistory | — | ✅ Redis + API required |
| SemanticRouter routing | — | ✅ Redis + API required |
| Vectorizer Embed methods | — | ✅ API calls required |

Many classes are tightly coupled to Redis operations. Full unit test coverage would require extracting interfaces for Redis operations and adding dependency injection, which is outside the current scope.

## Project Structure

```
├── src/RedisVL/
│   ├── Schema/           # Index schema definition and field types
│   ├── Index/            # SearchIndex, RedisConnectionProvider
│   ├── Query/            # VectorQuery, RangeQuery, HybridQuery, TextQuery, etc.
│   │   └── Filter/       # Tag, Numeric, Geo, Text filter expressions
│   ├── Extensions/
│   │   ├── Cache/        # SemanticCache
│   │   ├── MessageHistory/ # BaseMessageHistory, SemanticMessageHistory
│   │   └── Router/       # SemanticRouter, Route
│   ├── Utils/
│   │   ├── Vectorizers/  # OpenAI, AzureOpenAI, Cohere, HuggingFace
│   │   └── Rerankers/    # CohereReranker
│   └── Exceptions/       # RedisVLException hierarchy
├── tests/RedisVL.Tests/  # Unit tests
└── README.md
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request

## References

- [RedisVL for Python](https://github.com/redis/redis-vl-python) — The Python implementation this library is based on
- [Redis Stack documentation](https://redis.io/docs/latest/develop/interact/search-and-query/)
- [NRedisStack](https://github.com/redis/NRedisStack) — The underlying .NET Redis client

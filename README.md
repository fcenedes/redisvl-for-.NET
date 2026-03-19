# RedisVL for .NET

[![CI](https://github.com/redis/redis-vl-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/redis/redis-vl-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/RedisVL.svg)](https://www.nuget.org/packages/RedisVL)

A .NET client library for [Redis](https://redis.io/) vector search and AI-powered extensions, inspired by [RedisVL for Python](https://github.com/redis/redis-vl-python).

## Features

- **Schema-driven index management** — Define index schemas in YAML, JSON, or dictionaries; create/drop Redis search indexes
- **Vector similarity search** — KNN vector queries, range queries, and hybrid (vector + text) queries
- **Aggregate hybrid search** — Combined text + vector scoring via `AggregateHybridQuery` with configurable alpha weighting
- **Full-text search** — BM25 text search with field targeting and filters
- **Rich filter expressions** — Tag, Numeric, Geo, Text, and Timestamp filters with boolean combinators (`&`, `|`, `~`)
- **Semantic Cache** — Cache LLM responses by semantic similarity to reduce API costs
- **Embeddings Cache** — Cache raw embeddings by text + model key
- **Message History** — Store and retrieve LLM conversation history with optional semantic search and multi-role filtering
- **Semantic Router** — Classify queries into predefined routes using vector similarity
- **Multiple vectorizers** — OpenAI, Azure OpenAI, Cohere, HuggingFace, Vertex AI, Mistral, VoyageAI embedding integrations
- **Custom vectorizers** — Bring your own embedding function via `CustomTextVectorizer`
- **Reranking** — Cohere reranker integration for result refinement
- **Sentinel support** — Connect through Redis Sentinel for high-availability setups

## Requirements

- **.NET 10** (net10.0 target framework)
- **Redis 8+** with RediSearch module (for vector search and full-text search)
- API keys for vectorizer/reranker providers as needed

## Installation

```bash
dotnet add package RedisVL
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
var timestamp = Timestamp.Field("created_at") >= new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var dateRange = Timestamp.Field("updated_at").Between(startDate, endDate);
var combined = (tag & numeric & timestamp) | text;
```

### Queries

```csharp
// Vector KNN search
var vq = new VectorQuery(vector, "embedding", numResults: 10);

// Range search
var rq = new RangeQuery(vector, "embedding", distanceThreshold: 0.5);

// Hybrid vector + text search
var hq = new HybridQuery(vector, "embedding", "search text", "content");

// Aggregate hybrid search (text + vector with alpha weighting)
var ahq = new AggregateHybridQuery("search text", "content", vector, "embedding", alpha: 0.7);

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

// Embeddings Cache
var embCache = new EmbeddingsCache("redis://localhost:6379");
await embCache.SetAsync("hello", "text-embedding-3-small", embedding);
var entry = await embCache.GetAsync("hello", "text-embedding-3-small");

// Semantic Router
var router = new SemanticRouter("my-router", routes, vectorizer);
var match = await router.RouteAsync("Hello there!");

// Message History (with multi-role filtering)
var history = new SemanticMessageHistory("session-1", vectorizer);
await history.AddMessageAsync(new Message { Role = "user", Content = "Hi" });
var recent = await history.GetRecentAsync(topK: 10, roles: new[] { "user", "llm" });
var relevant = await history.GetRelevantAsync("greeting");
```

### Vectorizers

```csharp
// OpenAI (default)
var openai = new OpenAITextVectorizer(model: "text-embedding-3-small", apiKey: "sk-...");

// Azure OpenAI
var azure = new AzureOpenAITextVectorizer(deploymentName: "my-embedding", apiKey: "...");

// Vertex AI (Google)
var vertex = new VertexAITextVectorizer(model: "textembedding-gecko", apiKey: "...");

// Mistral
var mistral = new MistralTextVectorizer(model: "mistral-embed", apiKey: "...");

// VoyageAI
var voyage = new VoyageAITextVectorizer(model: "voyage-3-large", apiKey: "...");

// Custom (bring your own function)
var custom = new CustomTextVectorizer(text => MyEmbedFunc(text), dims: 768);
```

### Connection Options

```csharp
// Standard connection
var index = new SearchIndex(schema, "redis://localhost:6379");

// With authentication
var index = new SearchIndex(schema, "redis://user:password@host:6379/0");

// SSL connection
var index = new SearchIndex(schema, "rediss://host:6380");

// Sentinel connection
var index = new SearchIndex(schema, "sentinel://sentinel1:26379,sentinel2:26379/mymaster");
```

## Testing

Run unit tests (no Redis required):

```bash
dotnet test --filter "Category!=Integration"
```

Run integration tests (requires Redis on localhost:6379):

```bash
dotnet test --filter "Category=Integration"
```

Run all tests:

```bash
dotnet test
```

Run with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage

| Area | Unit Tests | Integration Tests |
| --- | --- | --- |
| Schema parsing (YAML, JSON, Dictionary) | ✅ 5 tests | — |
| Query string generation | ✅ 15+ tests | — |
| Filter expressions (Tag, Num, Geo, Text, Timestamp) | ✅ 20+ tests | — |
| Field attributes & FieldPath edge cases | ✅ 10 tests | — |
| Vectorizer constructors & config | ✅ 25+ tests | — |
| SearchResult / SearchResults defaults | ✅ 9 tests | — |
| RedisConnectionProvider URL parsing | ✅ 16 tests | — |
| AggregateHybridQuery | ✅ 7 tests | — |
| SearchIndex CRUD + Load/Fetch/Delete/Query | — | ✅ 10 tests |
| SemanticCache Store/Check/Clear | — | ✅ 4 tests |
| EmbeddingsCache Set/Get/MSet/MGet | — | ✅ 6 tests |
| BaseMessageHistory | — | ✅ 5 tests |
| SemanticMessageHistory | — | ✅ 3 tests |
| SemanticRouter routing | — | ✅ 5 tests |

## Project Structure

```
├── .github/workflows/
│   ├── ci.yml              # Build + unit tests on push/PR
│   ├── integration.yml     # Integration tests with Redis
│   └── publish.yml         # NuGet publish on version tags
├── src/RedisVL/
│   ├── Schema/             # Index schema definition and field types
│   ├── Index/              # SearchIndex, RedisConnectionProvider (incl. Sentinel)
│   ├── Query/              # VectorQuery, RangeQuery, HybridQuery, AggregateHybridQuery, etc.
│   │   └── Filter/         # Tag, Numeric, Geo, Text, Timestamp filter expressions
│   ├── Extensions/
│   │   ├── Cache/          # SemanticCache, EmbeddingsCache
│   │   ├── MessageHistory/ # BaseMessageHistory, SemanticMessageHistory
│   │   └── Router/         # SemanticRouter, Route
│   ├── Utils/
│   │   ├── Vectorizers/    # OpenAI, AzureOpenAI, Cohere, HuggingFace, VertexAI, Mistral, VoyageAI, Custom
│   │   └── Rerankers/      # CohereReranker
│   └── Exceptions/         # RedisVLException hierarchy
├── tests/RedisVL.Tests/    # Unit + integration tests
└── README.md
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass: `dotnet test --filter "Category!=Integration"`
5. Submit a pull request

## References

- [RedisVL for Python](https://github.com/redis/redis-vl-python) — The Python implementation this library is based on
- [Redis Stack documentation](https://redis.io/docs/latest/develop/interact/search-and-query/)
- [NRedisStack](https://github.com/redis/NRedisStack) — The underlying .NET Redis client
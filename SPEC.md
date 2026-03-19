# RedisVL .NET Core Library Specification

## Goal

Create a .NET Core version of the RedisVL Python library ([https://github.com/redis/redis-vl-python](https://github.com/redis/redis-vl-python)) with equivalent functionality for vector search, semantic caching, LLM memory, and AI extensions.

## Target Framework

- .NET 8.0
- C# 12

## Dependencies

- NRedisStack (for Redis with RediSearch/RedisJSON)
- YamlDotNet (for YAML schema parsing)
- System.Text.Json (built-in)

## Project Structure

```
src/
└── RedisVL/
    ├── RedisVL.csproj
    ├── Schema/
    │   ├── IndexSchema.cs
    │   ├── IndexInfo.cs
    │   └── Fields/
    │       ├── FieldBase.cs
    │       ├── TextField.cs
    │       ├── TagField.cs
    │       ├── NumericField.cs
    │       ├── GeoField.cs
    │       └── VectorField.cs
    ├── Index/
    │   ├── ISearchIndex.cs
    │   ├── SearchIndex.cs
    │   └── RedisConnectionProvider.cs
    ├── Query/
    │   ├── BaseQuery.cs
    │   ├── VectorQuery.cs
    │   ├── RangeQuery.cs
    │   ├── FilterQuery.cs
    │   ├── TextQuery.cs
    │   ├── HybridQuery.cs
    │   └── CountQuery.cs
    ├── Query/Filter/
    │   ├── FilterExpression.cs
    │   ├── TagFilter.cs
    │   ├── NumericFilter.cs
    │   ├── TextField.cs
    │   └── GeoFilter.cs
    ├── Extensions/
    │   ├── SemanticCache/
    │   │   └── SemanticCache.cs
    │   ├── MessageHistory/
    │   │   ├── BaseMessageHistory.cs
    │   │   └── SemanticMessageHistory.cs
    │   └── Router/
    │       ├── Route.cs
    │       ├── RouteMatch.cs
    │       └── SemanticRouter.cs
    ├── Utils/
    │   ├── Vectorizers/
    │   │   ├── ITextVectorizer.cs
    │   │   ├── OpenAITextVectorizer.cs
    │   │   ├── AzureOpenAITextVectorizer.cs
    │   │   ├── CohereTextVectorizer.cs
    │   │   └── HuggingFaceTextVectorizer.cs
    │   └── Rerankers/
    │       ├── IReranker.cs
    │       └── CohereReranker.cs
    └── Exceptions/
        └── RedisVLException.cs
tests/
└── RedisVL.Tests/
    └── RedisVL.Tests.csproj
```

## Key API Design (mirroring Python API)

### Schema Definition

```csharp
// From YAML
var schema = IndexSchema.FromYaml("schema.yaml");

// From dictionary/object
var schema = IndexSchema.FromDictionary(new {
    index = new { name = "user-idx", prefix = "user", storage_type = "json" },
    fields = new[] {
        new { name = "user", type = "tag" },
        new { name = "embedding", type = "vector", attrs = new { algorithm = "hnsw", dims = 1536, distance_metric = "cosine" } }
    }
});
```

### Index Operations

```csharp
var index = new SearchIndex(schema, "redis://localhost:6379");
await index.CreateAsync();
await index.LoadAsync(data, idField: "user");
var result = await index.FetchAsync("john");
await index.DropAsync();
```

### Vector Query

```csharp
var query = new VectorQuery(
    vector: embedding,
    vectorFieldName: "embedding",
    numResults: 10,
    returnFields: new[] { "content", "source" }
);
var results = await index.QueryAsync(query);
```

### Filters

```csharp
var tagFilter = Tag.Field("category") == "electronics";
var priceFilter = Num.Field("price") >= 100 & Num.Field("price") <= 500;
var combinedFilter = tagFilter & priceFilter;

var query = new VectorQuery(...) { FilterExpression = combinedFilter };
```

### Semantic Cache

```csharp
var cache = new SemanticCache(
    name: "llmcache",
    redisUrl: "redis://localhost:6379",
    ttl: TimeSpan.FromMinutes(60),
    distanceThreshold: 0.1
);
await cache.StoreAsync(prompt: "What is Redis?", response: "Redis is...");
var cached = await cache.CheckAsync(prompt: "Tell me about Redis");
```

### Message History

```csharp
var history = new SemanticMessageHistory(
    name: "my-session",
    redisUrl: "redis://localhost:6379",
    distanceThreshold: 0.7
);
await history.AddMessagesAsync(new[] {
    new Message { Role = "user", Content = "Hello" },
    new Message { Role = "llm", Content = "Hi there!" }
});
var recent = await history.GetRecentAsync(topK: 5);
var relevant = await history.GetRelevantAsync("weather", topK: 3);
```

### Semantic Router

```csharp
var routes = new[] {
    new Route("greeting", new[] { "hello", "hi" }, distanceThreshold: 0.3),
    new Route("farewell", new[] { "bye", "goodbye" }, distanceThreshold: 0.3)
};
var router = new SemanticRouter("topic-router", routes, "redis://localhost:6379");
var match = await router.RouteAsync("Hi, good morning");
// Returns: RouteMatch { Name = "greeting", Distance = 0.27 }
```

## Feature Parity Table

| Feature | Python RedisVL | C# RedisVL | Status |
|---------|---------------|------------|--------|
| **Schema** | | | |
| YAML schema loading | ✅ | ✅ | Complete |
| JSON schema loading | ✅ | ✅ | Complete |
| Dictionary schema loading | ✅ | ✅ | Complete |
| **Index** | | | |
| SearchIndex (Hash) | ✅ | ✅ | Complete |
| SearchIndex (JSON) | ✅ | ✅ | Complete |
| Create/Drop/Exists | ✅ | ✅ | Complete |
| Load/Fetch/Delete | ✅ | ✅ | Complete |
| **Query** | | | |
| VectorQuery | ✅ | ✅ | Complete |
| RangeQuery | ✅ | ✅ | Complete |
| FilterQuery | ✅ | ✅ | Complete |
| TextQuery | ✅ | ✅ | Complete |
| HybridQuery | ✅ | ✅ | Complete |
| CountQuery | ✅ | ✅ | Complete |
| **Filters** | | | |
| TagFilter | ✅ | ✅ | Complete |
| NumericFilter | ✅ | ✅ | Complete |
| TextField | ✅ | ✅ | Complete |
| GeoFilter | ✅ | ✅ | Complete |
| **Vectorizers** | | | |
| OpenAI | ✅ | ✅ | Complete |
| Azure OpenAI | ✅ | ✅ | Complete |
| Cohere | ✅ | ✅ | Complete |
| HuggingFace | ✅ | ✅ | Complete |
| CustomTextVectorizer | ✅ | ✅ | Complete |
| **Extensions** | | | |
| SemanticCache | ✅ | ✅ | Complete |
| EmbeddingsCache | ✅ | ✅ | Complete |
| BaseMessageHistory | ✅ | ✅ | Complete |
| SemanticMessageHistory | ✅ | ✅ | Complete |
| SemanticRouter | ✅ | ✅ | Complete |
| **Rerankers** | | | |
| CohereReranker | ✅ | ✅ | Complete |
| **Tutorial App** | N/A | ✅ | Complete |

## Tutorial App Section

### Avalonia UI Tutorial App

An interactive desktop application built with Avalonia UI to demonstrate RedisVL .NET capabilities.

**Location:** `samples/RedisVL.Tutorial/`

**Target Framework:** .NET 8.0 with Avalonia UI

**Structure:**
```
samples/
└── RedisVL.Tutorial/
    ├── RedisVL.Tutorial.csproj  (Avalonia app, net8.0)
    ├── App.axaml
    ├── MainWindow.axaml
    ├── ViewModels/
    │   └── MainWindowViewModel.cs
    └── Views/
        ├── SemanticCacheDemo.axaml
        ├── EmbeddingsCacheDemo.axaml
        └── MessageHistoryDemo.axaml
```

**Features Demonstrated:**

1. **SemanticCache Demo** - Store and check LLM responses with semantic similarity matching. Shows how prompts with similar meaning return cached responses.

2. **EmbeddingsCache Demo** - Cache and retrieve raw embeddings by text and model name. Demonstrates batch operations (MSet/MGet) and TTL management.

3. **MessageHistory Demo** - Conversation history with semantic search. Shows adding messages and retrieving relevant context by semantic similarity.

4. **CustomTextVectorizer** - User-provided embedding function integration. Demonstrates plugging in custom embedding logic without external API dependencies.

## Implementation Status

- [x] Project Setup & Structure
- [x] Schema Module
- [x] Index Module
- [x] Query Module
- [x] Filter Module
- [x] Vectorizers Module
- [x] Extensions - Semantic Cache
- [x] Extensions - Message History
- [x] Extensions - Semantic Router
- [x] Extensions - EmbeddingsCache
- [x] Utils - CustomTextVectorizer
- [x] Tutorial App (Avalonia UI)
- [ ] Unit Tests

## References

- Python RedisVL: [https://github.com/redis/redis-vl-python](https://github.com/redis/redis-vl-python)
- RedisVL Docs: [https://docs.redisvl.com/](https://docs.redisvl.com/)
- NRedisStack: [https://github.com/redis/NRedisStack](https://github.com/redis/NRedisStack)
# RedisVL for .NET — Documentation

Welcome to the documentation for **RedisVL for .NET**, a client library for Redis vector search and AI-powered extensions.

## Quick Navigation

### Getting Started
- **[Getting Started](getting-started.md)** — Installation, prerequisites, and your first vector search

### User Guide
- **[Schema Definition](user-guide/schema.md)** — Define index schemas in YAML, JSON, or code
- **[Queries](user-guide/queries.md)** — Vector, text, hybrid, range, filter, and aggregate queries
- **[Filters](user-guide/filters.md)** — Tag, Numeric, Geo, Text, and Timestamp filter expressions
- **[Vectorizers](user-guide/vectorizers.md)** — OpenAI, Azure OpenAI, Cohere, HuggingFace, Vertex AI, Mistral, VoyageAI, and custom vectorizers
- **[Extensions](user-guide/extensions.md)** — Semantic Cache, Embeddings Cache, Message History, and Semantic Router
- **[Connections](user-guide/connections.md)** — Redis URLs, SSL, Sentinel, and connection management

### Tutorial
- **[Interactive Tutorial App](tutorial.md)** — Avalonia desktop app demonstrating Semantic Cache, Embeddings Cache, and Message History with step-by-step walkthroughs

### Reference
- **[API Reference](api/reference.md)** — Complete API documentation for all public classes and methods

## Overview

RedisVL for .NET brings the power of Redis as a vector database to the .NET ecosystem. It provides:

- **Schema-driven index management** with YAML, JSON, and dictionary support
- **Multiple query types** — KNN vector search, range queries, hybrid text+vector, full-text BM25, aggregate hybrid scoring
- **Rich filter expressions** — composable Tag, Numeric, Geo, Text, and Timestamp filters
- **AI extensions** — Semantic Cache, Embeddings Cache, Message History, Semantic Router
- **7 vectorizer integrations** — OpenAI, Azure OpenAI, Cohere, HuggingFace, Vertex AI, Mistral, VoyageAI
- **High availability** — Redis Sentinel connection support

## Requirements

| Requirement | Version |
|---|---|
| .NET | 10.0+ |
| Redis | 8.0+ with RediSearch module |
| NuGet Package | `RedisVL` |

## Quick Example

```csharp
using RedisVL.Schema;
using RedisVL.Index;
using RedisVL.Query;
using RedisVL.Query.Filter;

// Define schema
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

// Create index and search
using var index = new SearchIndex(schema, "redis://localhost:6379");
await index.CreateAsync(overwrite: true);

var results = await index.QueryAsync(new VectorQuery(queryVector, "embedding", 5)
{
    FilterExpression = Tag.Field("category") == "electronics"
});
```

## License

MIT — see [LICENSE](../LICENSE) for details.


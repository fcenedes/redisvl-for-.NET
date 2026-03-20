# Getting Started

## Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download)
- **Redis 8+** with RediSearch module — [Redis Stack](https://redis.io/docs/latest/operate/oss_and_stack/install/install-stack/) or [Redis Cloud](https://redis.io/cloud/)

### Quick Redis Setup

```bash
# Docker (recommended for development)
docker run -d --name redis-stack -p 6379:6379 redis/redis-stack-server:latest

# Verify
redis-cli ping
# → PONG
```

## Installation

```bash
dotnet add package RedisVL
```

Or add a project reference if building from source:

```bash
dotnet add reference src/RedisVL/RedisVL.csproj
```

## Your First Vector Search

### Step 1: Define a Schema

```csharp
using RedisVL.Schema;

var schema = IndexSchema.FromJson(@"{
    ""index"": {
        ""name"": ""my-index"",
        ""prefix"": ""doc"",
        ""storage_type"": ""hash""
    },
    ""fields"": [
        { ""name"": ""content"", ""type"": ""text"" },
        { ""name"": ""category"", ""type"": ""tag"" },
        { ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": {
            ""algorithm"": ""hnsw"",
            ""dims"": 3,
            ""distance_metric"": ""cosine""
        }}
    ]
}");
```

### Step 2: Create the Index

```csharp
using RedisVL.Index;

using var index = new SearchIndex(schema, "redis://localhost:6379");
await index.CreateAsync(overwrite: true);
```

### Step 3: Load Data

```csharp
var documents = new[]
{
    new Dictionary<string, object>
    {
        ["id"] = "doc1",
        ["content"] = "Redis is a fast in-memory database",
        ["category"] = "database",
        ["embedding"] = new float[] { 0.1f, 0.2f, 0.3f }
    },
    new Dictionary<string, object>
    {
        ["id"] = "doc2",
        ["content"] = "PostgreSQL is a relational database",
        ["category"] = "database",
        ["embedding"] = new float[] { 0.4f, 0.5f, 0.6f }
    }
};

await index.LoadAsync(documents, idField: "id");
```

### Step 4: Search

```csharp
using RedisVL.Query;
using RedisVL.Query.Filter;

// Vector similarity search
var queryVector = new float[] { 0.1f, 0.2f, 0.3f };
var results = await index.QueryAsync(new VectorQuery(queryVector, "embedding", numResults: 5));

foreach (var doc in results.Documents)
{
    Console.WriteLine($"{doc.Id}: {doc.GetField<string>("content")} (distance: {doc.Score})");
}

// With filters
var filtered = await index.QueryAsync(new VectorQuery(queryVector, "embedding", 5)
{
    FilterExpression = Tag.Field("category") == "database"
});
```

### Step 5: Clean Up

```csharp
await index.DropAsync(dropDocuments: true);
```

## Using a Vectorizer

Instead of providing embeddings manually, use a vectorizer:

```csharp
using RedisVL.Utils.Vectorizers;

// OpenAI
var vectorizer = new OpenAITextVectorizer(
    model: "text-embedding-3-small",
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
);

// Generate embeddings
var embedding = await vectorizer.EmbedAsync("Redis is fast");
var embeddings = await vectorizer.EmbedManyAsync(new[] { "text 1", "text 2" });
```

## Next Steps

- **[Schema Definition](user-guide/schema.md)** — Learn about YAML, JSON, and dictionary schemas
- **[Queries](user-guide/queries.md)** — Explore all query types
- **[Filters](user-guide/filters.md)** — Build complex filter expressions
- **[Extensions](user-guide/extensions.md)** — Use Semantic Cache, Message History, and more
- **[API Reference](api/reference.md)** — Complete API documentation


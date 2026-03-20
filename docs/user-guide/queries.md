# Queries

RedisVL provides multiple query types for different search patterns. All queries extend `BaseQuery` and share common properties.

## Common Properties (BaseQuery)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FilterExpression` | `FilterExpression?` | `null` | Filter to apply |
| `ReturnFields` | `string[]?` | `null` | Fields to return (null = all) |
| `NumResults` | `int` | `10` | Max results to return |
| `Offset` | `int` | `0` | Pagination offset |
| `Dialect` | `int` | `2` | RediSearch dialect version |

## VectorQuery — KNN Similarity Search

Finds the K nearest neighbors to a query vector.

```csharp
var query = new VectorQuery(queryVector, "embedding", numResults: 10)
{
    FilterExpression = Tag.Field("category") == "tech",
    ReturnFields = new[] { "title", "content" },
    ScoreFieldName = "vector_distance",  // default
    EfRuntime = 20  // optional HNSW parameter
};

var results = await index.QueryAsync(query);
foreach (var doc in results.Documents)
{
    Console.WriteLine($"{doc.Id}: distance={doc.Score}");
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Vector` | `float[]` | `[]` | Query vector |
| `VectorFieldName` | `string` | `"embedding"` | Vector field to search |
| `ScoreFieldName` | `string` | `"vector_distance"` | Name for distance in results |
| `EfRuntime` | `int?` | `null` | HNSW search depth override |
| `ReturnScore` | `bool` | `true` | Include distance in results |

## RangeQuery — Distance Threshold Search

Finds all vectors within a specified distance.

```csharp
var query = new RangeQuery(queryVector, "embedding", distanceThreshold: 0.3)
{
    FilterExpression = Num.Field("price") <= 100,
    NumResults = 50
};
var results = await index.QueryAsync(query);
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Vector` | `float[]` | `[]` | Query vector |
| `VectorFieldName` | `string` | `"embedding"` | Vector field to search |
| `DistanceThreshold` | `double` | `0.2` | Max distance for results |

## TextQuery — Full-Text Search

BM25 full-text search with optional field targeting.

```csharp
// Search all text fields
var query = new TextQuery("redis database");

// Search a specific field
var query = new TextQuery("redis database", "content")
{
    SortBy = "created_at",
    SortAscending = false
};
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | Search text |
| `TextFieldName` | `string?` | `null` | Target field (null = all) |
| `SortBy` | `string?` | `null` | Sort field |
| `SortAscending` | `bool` | `true` | Sort direction |

## HybridQuery — Vector + Text (Redis 8.4+)

Combines vector similarity and text search with configurable scoring.

```csharp
var query = new HybridQuery(queryVector, "embedding", "search terms", "content")
{
    CombinationMethod = HybridCombinationMethod.Linear,
    VectorWeight = 0.7,
    TextWeight = 0.3,
    NumResults = 10
};
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Vector` | `float[]` | `[]` | Query vector |
| `VectorFieldName` | `string` | `"embedding"` | Vector field |
| `Text` | `string` | `""` | Search text |
| `TextFieldName` | `string` | `"content"` | Text field |
| `CombinationMethod` | `HybridCombinationMethod` | `Linear` | `Linear` or `RRF` |
| `VectorWeight` | `double` | `0.5` | Vector score weight |
| `TextWeight` | `double` | `0.5` | Text score weight |

## AggregateHybridQuery — Weighted Text + Vector

Combines text and vector search using FT.AGGREGATE with alpha weighting.

```csharp
var query = new AggregateHybridQuery("search text", "content", queryVector, "embedding")
{
    Alpha = 0.7,        // vector weight (text weight = 1 - alpha)
    TextScorer = "BM25STD",
    NumResults = 10
};

// Query string format:
// @content:(search text)=>[KNN 10 @embedding $vector AS vector_distance]
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | Search text |
| `TextFieldName` | `string` | `"content"` | Text field |
| `Vector` | `float[]` | `[]` | Query vector |
| `VectorFieldName` | `string` | `"embedding"` | Vector field |
| `Alpha` | `double` | `0.7` | Vector weight (0–1) |
| `TextScorer` | `string` | `"BM25STD"` | Text scoring algorithm |

## FilterQuery — Filter-Only Search

Search using only filter expressions (no vector or text).

```csharp
var query = new FilterQuery(Tag.Field("status") == "active")
{
    SortBy = "created_at",
    SortAscending = false,
    NumResults = 20
};
```

## CountQuery — Count Matching Documents

```csharp
// Count all documents
var total = await index.CountAsync();

// Count with filter
var active = await index.CountAsync(new CountQuery(Tag.Field("status") == "active"));
```

## Working with Results

```csharp
SearchResults results = await index.QueryAsync(query);

results.TotalResults;    // Total matching documents
results.Documents;       // List<SearchResult>

foreach (var doc in results.Documents)
{
    doc.Id;                              // Document ID
    doc.Score;                           // Distance/score (nullable)
    doc.Fields;                          // Dictionary<string, object?>
    doc.GetField<string>("title");       // Typed field access
    doc.GetField<int>("count");          // Auto-converts strings to int
    doc.GetField<double>("price");       // Auto-converts strings to double
}
```


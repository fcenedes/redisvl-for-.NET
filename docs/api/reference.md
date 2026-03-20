# API Reference

Complete API documentation for all public classes in RedisVL for .NET.

## Table of Contents

- [Index](#index)
  - [SearchIndex](#searchindex)
  - [RedisConnectionProvider](#redisconnectionprovider)
- [Schema](#schema)
  - [IndexSchema](#indexschema)
  - [Field Types](#field-types)
- [Queries](#queries)
  - [VectorQuery](#vectorquery)
  - [RangeQuery](#rangequery)
  - [TextQuery](#textquery)
  - [HybridQuery](#hybridquery)
  - [AggregateHybridQuery](#aggregatehybridquery)
  - [FilterQuery](#filterquery)
  - [CountQuery](#countquery)
  - [SearchResults / SearchResult](#searchresults)
- [Filters](#filters)
  - [Tag](#tag)
  - [Num](#num)
  - [Timestamp](#timestamp)
  - [Geo](#geo)
  - [Text](#text)
  - [FilterExpression](#filterexpression)
- [Extensions](#extensions)
  - [SemanticCache](#semanticcache)
  - [EmbeddingsCache](#embeddingscache)
  - [BaseMessageHistory](#basemessagehistory)
  - [SemanticMessageHistory](#semanticmessagehistory)
  - [SemanticRouter](#semanticrouter)
  - [Route](#route)
  - [Message](#message)
- [Vectorizers](#vectorizers)
  - [ITextVectorizer](#itextvectorizer)
  - [OpenAITextVectorizer](#openaitextvectorizer)
  - [AzureOpenAITextVectorizer](#azureopenaitextvectorizer)
  - [CohereTextVectorizer](#coheretextvectorizer)
  - [HuggingFaceTextVectorizer](#huggingfacetextvectorizer)
  - [VertexAITextVectorizer](#vertexaitextvectorizer)
  - [MistralTextVectorizer](#mistraltextvectorizer)
  - [VoyageAITextVectorizer](#voyageaitextvectorizer)
  - [CustomTextVectorizer](#customtextvectorizer)
- [Rerankers](#rerankers)
  - [CohereReranker](#coherereranker)
- [Exceptions](#exceptions)

---

## Index

### SearchIndex

`RedisVL.Index.SearchIndex` — Manages a Redis search index.

**Constructors:**

```csharp
SearchIndex(IndexSchema schema, string redisUrl)
SearchIndex(IndexSchema schema, RedisConnectionProvider provider)
```

**Methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateAsync(bool overwrite = false)` | `Task` | Create the index in Redis |
| `DropAsync(bool dropDocuments = false)` | `Task` | Drop the index |
| `ExistsAsync()` | `Task<bool>` | Check if index exists |
| `InfoAsync()` | `Task<Dictionary<string, object>>` | Get index info |
| `LoadAsync(IEnumerable<Dictionary<string, object>> data, string? idField = null, string[]? keys = null)` | `Task` | Load documents |
| `FetchAsync(string id)` | `Task<Dictionary<string, object>?>` | Fetch document by ID |
| `DeleteAsync(string[] ids)` | `Task` | Delete documents by ID |
| `QueryAsync(BaseQuery query)` | `Task<SearchResults>` | Execute a query |
| `CountAsync(CountQuery? query = null)` | `Task<long>` | Count documents |
| `Dispose()` | `void` | Dispose connection |

### RedisConnectionProvider

`RedisVL.Index.RedisConnectionProvider` — Manages Redis connections.

**Constructors:**

```csharp
RedisConnectionProvider(string redisUrl)
RedisConnectionProvider(ConnectionMultiplexer connection)
RedisConnectionProvider(ConfigurationOptions options)
RedisConnectionProvider(string serviceName, IEnumerable<EndPoint> sentinelEndpoints, string? password = null)
```

**Methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `GetDatabase(int db = -1)` | `IDatabase` | Get Redis database |
| `GetConnection()` | `ConnectionMultiplexer` | Get underlying connection |
| `Dispose()` | `void` | Dispose connection |

---

## Schema

### IndexSchema

`RedisVL.Schema.IndexSchema` — Defines the structure of a search index.

**Static Factory Methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `FromJson(string json)` | `IndexSchema` | Parse from JSON string |
| `FromYaml(string filePath)` | `IndexSchema` | Parse from YAML file |
| `FromYamlString(string yaml)` | `IndexSchema` | Parse from YAML string |
| `FromDictionary(Dictionary<string, object> dict)` | `IndexSchema` | Parse from dictionary |

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Index` | `IndexInfo` | Index metadata (name, prefix, storage type) |
| `Fields` | `List<FieldBase>` | Field definitions |

**IndexInfo Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Index name |
| `Prefix` | `string` | Key prefix |
| `StorageType` | `StorageType` | `Hash` or `Json` |

### Field Types

All fields extend `FieldBase`.

**FieldBase Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Field name |
| `Path` | `string?` | JSON path override |
| `FieldPath` | `string` | Computed path (`$.name` or explicit) |

**Concrete Types:** `TextField`, `TagField`, `NumericField`, `GeoField`, `VectorField`

**VectorField Properties:**

| Property | Type | Default |
|----------|------|---------|
| `Algorithm` | `VectorAlgorithm` | `HNSW` |
| `Dims` | `int` | `0` |
| `DistanceMetric` | `DistanceMetric` | `Cosine` |
| `DataType` | `string` | `"FLOAT32"` |
| `M` | `int` | `16` |
| `EfConstruction` | `int` | `200` |
| `EfRuntime` | `int` | `10` |
| `BlockSize` | `int` | `1024` |

---

## Queries

### VectorQuery

`RedisVL.Query.VectorQuery` — KNN vector similarity search.

```csharp
new VectorQuery(float[] vector, string vectorFieldName, int numResults = 10)
```

| Property | Type | Default |
|----------|------|---------|
| `Vector` | `float[]` | — |
| `VectorFieldName` | `string` | — |
| `ScoreFieldName` | `string` | `"vector_distance"` |
| `EfRuntime` | `int?` | `null` |
| `ReturnScore` | `bool` | `true` |
| *Inherited from BaseQuery* | | |
| `FilterExpression` | `FilterExpression?` | `null` |
| `ReturnFields` | `string[]?` | `null` |
| `NumResults` | `int` | `10` |
| `Offset` | `int` | `0` |

### RangeQuery

`RedisVL.Query.RangeQuery` — Distance threshold vector search.

```csharp
new RangeQuery(float[] vector, string vectorFieldName, double distanceThreshold = 0.2)
```

| Property | Type | Default |
|----------|------|---------|
| `DistanceThreshold` | `double` | `0.2` |

### TextQuery

`RedisVL.Query.TextQuery` — Full-text BM25 search.

```csharp
new TextQuery(string text, string? textFieldName = null)
```

| Property | Type | Default |
|----------|------|---------|
| `Text` | `string` | — |
| `TextFieldName` | `string?` | `null` |
| `SortBy` | `string?` | `null` |
| `SortAscending` | `bool` | `true` |

### HybridQuery

`RedisVL.Query.HybridQuery` — Combined vector + text search.

```csharp
new HybridQuery(float[] vector, string vectorFieldName, string text, string textFieldName)
```

| Property | Type | Default |
|----------|------|---------|
| `CombinationMethod` | `HybridCombinationMethod` | `Linear` |
| `VectorWeight` | `double` | `0.5` |
| `TextWeight` | `double` | `0.5` |

### AggregateHybridQuery

`RedisVL.Query.AggregateHybridQuery` — Weighted text + vector via FT.AGGREGATE.

```csharp
new AggregateHybridQuery(string text, string textFieldName, float[] vector, string vectorFieldName, double alpha = 0.7, string textScorer = "BM25STD")
```

| Property | Type | Default |
|----------|------|---------|
| `Alpha` | `double` | `0.7` |
| `TextScorer` | `string` | `"BM25STD"` |
| `DistanceFieldName` | `string` | `"vector_distance"` |

**Methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `GetQueryString()` | `string` | Generate the query string |
| `GetScoringExpression()` | `string` | Get the hybrid scoring expression |
| `GetVectorSimilarityExpression()` | `string` | Get vector similarity transform |
| `GetVectorBytes()` | `byte[]` | Get vector as byte array |

### FilterQuery

`RedisVL.Query.FilterQuery` — Filter-only search (no vector/text).

```csharp
new FilterQuery(FilterExpression filter)
```

### CountQuery

`RedisVL.Query.CountQuery` — Count matching documents.

```csharp
new CountQuery(FilterExpression? filter = null)
```

### SearchResults

`RedisVL.Query.SearchResults` — Query result container.

| Property | Type | Default |
|----------|------|---------|
| `TotalResults` | `long` | `0` |
| `Documents` | `List<SearchResult>` | `[]` |
| `ExecutionTimeMs` | `double?` | `null` |

**SearchResult:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Document ID |
| `Score` | `double?` | Distance/score |
| `Fields` | `Dictionary<string, object?>` | Document fields |

| Method | Returns | Description |
|--------|---------|-------------|
| `GetField<T>(string key)` | `T` | Get typed field value |

---

## Filters

### Tag

```csharp
Tag.Field("fieldName") == "value"       // equals
Tag.Field("fieldName") != "value"       // not equals
Tag.Field("fieldName").In("a", "b")     // IN (multiple values)
```

### Num

```csharp
Num.Field("fieldName") == 5             // equals
Num.Field("fieldName") != 5             // not equals
Num.Field("fieldName") > 5              // greater than
Num.Field("fieldName") >= 5             // greater than or equal
Num.Field("fieldName") < 5              // less than
Num.Field("fieldName") <= 5             // less than or equal
Num.Field("fieldName").Between(1, 10)   // range (inclusive)
```

### Timestamp

Accepts `DateTime`, `DateTimeOffset`, or `long` (Unix seconds).

```csharp
Timestamp.Field("fieldName") == dateTime        // equals (date-only = entire day)
Timestamp.Field("fieldName") != dateTime        // not equals
Timestamp.Field("fieldName") > dateTime         // after
Timestamp.Field("fieldName") >= dateTime        // at or after
Timestamp.Field("fieldName") < dateTime         // before
Timestamp.Field("fieldName") <= dateTime        // at or before
Timestamp.Field("fieldName").Between(start, end) // range
```

### Geo

```csharp
Geo.Field("fieldName").WithinRadius(longitude, latitude, radius, GeoUnit.Kilometers)
```

### Text

```csharp
Text.Field("fieldName").Match("search terms")
```

### FilterExpression

Combine filters with boolean operators:

```csharp
var combined = filter1 & filter2;   // AND
var either = filter1 | filter2;     // OR
var negated = ~filter1;             // NOT
```

---

## Extensions

### SemanticCache

`RedisVL.Extensions.Cache.SemanticCache`

```csharp
new SemanticCache(string indexName, ITextVectorizer vectorizer, string redisUrl, double distanceThreshold = 0.1)
```

| Method | Returns | Description |
|--------|---------|-------------|
| `StoreAsync(string prompt, string response, Dictionary<string, string>? metadata = null)` | `Task` | Cache a response |
| `CheckAsync(string prompt, int numResults = 1)` | `Task<List<CacheHit>>` | Check for cached responses |
| `ClearAsync()` | `Task` | Clear all cached entries |
| `Dispose()` | `void` | Dispose resources |

**CacheHit Properties:** `Response` (string), `Metadata` (Dictionary?), `Distance` (double)

### EmbeddingsCache

`RedisVL.Extensions.Cache.EmbeddingsCache`

```csharp
new EmbeddingsCache(string redisUrl, string prefix = "emb")
```

| Method | Returns | Description |
|--------|---------|-------------|
| `SetAsync(string text, string modelName, float[] embedding, Dictionary<string, string>? metadata = null)` | `Task` | Cache an embedding |
| `GetAsync(string text, string modelName)` | `Task<EmbeddingEntry?>` | Get cached embedding |
| `ExistsAsync(string text, string modelName)` | `Task<bool>` | Check if cached |
| `DropAsync(string text, string modelName)` | `Task` | Remove entry |
| `MSetAsync(...)` | `Task` | Batch set |
| `MGetAsync(...)` | `Task<List<EmbeddingEntry?>>` | Batch get |
| `MExistsAsync(...)` | `Task<List<bool>>` | Batch exists |
| `MDropAsync(...)` | `Task` | Batch drop |
| `ClearAsync()` | `Task` | Clear all |

### BaseMessageHistory

`RedisVL.Extensions.MessageHistory.BaseMessageHistory`

```csharp
new BaseMessageHistory(string sessionId, string redisUrl)
```

| Method | Returns | Description |
|--------|---------|-------------|
| `AddMessageAsync(Message message)` | `Task` | Add a message |
| `AddMessagesAsync(IEnumerable<Message> messages)` | `Task` | Add multiple messages |
| `GetRecentAsync(int topK = 5, string? role = null)` | `Task<IList<Message>>` | Get recent messages |
| `GetRecentAsync(int topK, string[]? roles)` | `Task<IList<Message>>` | Get recent with multi-role filter |
| `CountAsync()` | `Task<long>` | Count messages |
| `ClearAsync()` | `Task` | Clear history |

### SemanticMessageHistory

`RedisVL.Extensions.MessageHistory.SemanticMessageHistory` — Extends `BaseMessageHistory`.

```csharp
new SemanticMessageHistory(string sessionId, ITextVectorizer vectorizer, string redisUrl, double distanceThreshold = 0.5)
```

| Method | Returns | Description |
|--------|---------|-------------|
| `GetRelevantAsync(string query, int topK = 5, string? role = null)` | `Task<IList<Message>>` | Semantic search over history |
| *All BaseMessageHistory methods* | | |

### SemanticRouter

`RedisVL.Extensions.Router.SemanticRouter`

```csharp
new SemanticRouter(string indexName, List<Route> routes, ITextVectorizer vectorizer, string redisUrl)
```

| Method | Returns | Description |
|--------|---------|-------------|
| `RouteAsync(string query)` | `Task<RouteMatch?>` | Classify a query |
| `InvokeAsync(string query)` | `Task<RouteMatch?>` | Alias for RouteAsync |
| `ClearAsync()` | `Task` | Clear route data |

**RouteMatch Properties:** `Name` (string), `Distance` (double)

### Route

`RedisVL.Extensions.Router.Route`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Route identifier |
| `References` | `List<string>` | Reference phrases |
| `DistanceThreshold` | `double` | Max distance for match |
| `Metadata` | `Dictionary<string, string>?` | Optional metadata |

### Message

`RedisVL.Extensions.MessageHistory.Message`

| Property | Type | Description |
|----------|------|-------------|
| `Role` | `string` | Message role (`"user"`, `"llm"`, `"system"`) |
| `Content` | `string` | Message content |
| `Metadata` | `Dictionary<string, string>?` | Optional metadata |

---

## Vectorizers

### ITextVectorizer

```csharp
public interface ITextVectorizer
{
    string Model { get; }
    int Dims { get; }
    Task<float[]> EmbedAsync(string text, string? inputType = null);
    Task<IList<float[]>> EmbedManyAsync(IList<string> texts, string? inputType = null);
}
```

### OpenAITextVectorizer

```csharp
new OpenAITextVectorizer(string model = "text-embedding-3-small", string? apiKey = null, string? apiUrl = null, int dims = 0, HttpClient? httpClient = null)
```
Env var: `OPENAI_API_KEY`

### AzureOpenAITextVectorizer

```csharp
new AzureOpenAITextVectorizer(string deploymentName, string? resourceName = null, string? apiKey = null, string apiVersion = "2024-02-01", int dims = 0, HttpClient? httpClient = null)
```
Env vars: `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_RESOURCE_NAME`

### CohereTextVectorizer

```csharp
new CohereTextVectorizer(string model = "embed-english-v3.0", string? apiKey = null, string inputType = "search_document", int dims = 0, HttpClient? httpClient = null)
```
Env var: `COHERE_API_KEY`

### HuggingFaceTextVectorizer

```csharp
new HuggingFaceTextVectorizer(string model = "sentence-transformers/all-MiniLM-L6-v2", string? apiKey = null, int dims = 0, HttpClient? httpClient = null)
```
Env var: `HUGGINGFACE_API_KEY`

### VertexAITextVectorizer

```csharp
new VertexAITextVectorizer(string model = "textembedding-gecko", string? apiKey = null, string? apiUrl = null, int dims = 0, HttpClient? httpClient = null)
```
Env var: `GOOGLE_API_KEY`

### MistralTextVectorizer

```csharp
new MistralTextVectorizer(string model = "mistral-embed", string? apiKey = null, string apiUrl = "https://api.mistral.ai/v1/embeddings", int dims = 0, HttpClient? httpClient = null)
```
Env var: `MISTRAL_API_KEY`

### VoyageAITextVectorizer

```csharp
new VoyageAITextVectorizer(string model = "voyage-3-large", string? apiKey = null, string apiUrl = "https://api.voyageai.com/v1/embeddings", int dims = 0, HttpClient? httpClient = null)
```
Env var: `VOYAGE_API_KEY`. Supports `inputType` parameter in `EmbedAsync`/`EmbedManyAsync`.

### CustomTextVectorizer

```csharp
new CustomTextVectorizer(Func<string, Task<float[]>> embedFunc, int dims, string model = "custom")
```

---

## Rerankers

### CohereReranker

`RedisVL.Utils.Rerankers.CohereReranker`

```csharp
new CohereReranker(string model = "rerank-english-v3.0", string? apiKey = null, HttpClient? httpClient = null)
```
Env var: `COHERE_API_KEY`

| Method | Returns | Description |
|--------|---------|-------------|
| `RerankAsync(string query, IList<string> documents, int topN = 5)` | `Task<IList<RerankResult>>` | Rerank documents |

**RerankResult Properties:** `Index` (int), `RelevanceScore` (double), `Document` (string)

---

## Exceptions

| Exception | Description |
|-----------|-------------|
| `RedisVLException` | Base exception for all RedisVL errors |
| `VectorizationException` | Vectorizer errors (missing API key, API failure) |
| `IndexException` | Index operation errors |
| `SchemaException` | Schema parsing errors |


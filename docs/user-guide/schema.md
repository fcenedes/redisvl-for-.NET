# Schema Definition

RedisVL uses schema-driven index management. Define your index schema in YAML, JSON, or code, and RedisVL handles the Redis index creation.

## Schema Formats

### JSON

```csharp
var schema = IndexSchema.FromJson(@"{
    ""index"": {
        ""name"": ""products"",
        ""prefix"": ""product"",
        ""storage_type"": ""hash""
    },
    ""fields"": [
        { ""name"": ""title"", ""type"": ""text"", ""attrs"": { ""sortable"": true, ""weight"": 2.0 } },
        { ""name"": ""category"", ""type"": ""tag"", ""attrs"": { ""separator"": "","" } },
        { ""name"": ""price"", ""type"": ""numeric"", ""attrs"": { ""sortable"": true } },
        { ""name"": ""location"", ""type"": ""geo"" },
        { ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": {
            ""algorithm"": ""hnsw"",
            ""dims"": 1536,
            ""distance_metric"": ""cosine"",
            ""datatype"": ""FLOAT32""
        }}
    ]
}");
```

### YAML File

```yaml
# schema.yaml
index:
  name: products
  prefix: product
  storage_type: hash

fields:
  - name: title
    type: text
    attrs:
      sortable: true
  - name: category
    type: tag
  - name: price
    type: numeric
    attrs:
      sortable: true
  - name: embedding
    type: vector
    attrs:
      algorithm: hnsw
      dims: 1536
      distance_metric: cosine
```

```csharp
var schema = IndexSchema.FromYaml("schema.yaml");
// Or from a YAML string:
var schema = IndexSchema.FromYamlString(yamlContent);
```

### Dictionary

```csharp
var dict = new Dictionary<string, object>
{
    ["index"] = new Dictionary<string, object>
    {
        ["name"] = "products",
        ["prefix"] = "product",
        ["storage_type"] = "hash"
    },
    ["fields"] = new object[]
    {
        new Dictionary<string, object> { ["name"] = "title", ["type"] = "text" },
        new Dictionary<string, object> { ["name"] = "category", ["type"] = "tag" }
    }
};
var schema = IndexSchema.FromDictionary(dict);
```

## Storage Types

| Type | Description | Use Case |
|------|-------------|----------|
| `hash` | Redis Hash (default) | Flat key-value documents |
| `json` | Redis JSON | Nested/complex documents |

## Field Types

### TextField

Full-text searchable field with BM25 scoring.

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `sortable` | bool | `false` | Enable sorting |
| `no_stem` | bool | `false` | Disable stemming |
| `weight` | double | `1.0` | Scoring weight |
| `no_index` | bool | `false` | Store but don't index |
| `phonetic` | string? | `null` | Phonetic matching algorithm |

### TagField

Exact-match tag field for filtering.

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `separator` | string | `","` | Tag separator character |
| `case_sensitive` | bool | `false` | Case-sensitive matching |
| `sortable` | bool | `false` | Enable sorting |

### NumericField

Numeric field for range queries and sorting.

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `sortable` | bool | `false` | Enable sorting |
| `no_index` | bool | `false` | Store but don't index |

### GeoField

Geographic coordinate field for radius queries.

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `sortable` | bool | `false` | Enable sorting |

### VectorField

Vector embedding field for similarity search.

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `algorithm` | string | `"hnsw"` | `"hnsw"` or `"flat"` |
| `dims` | int | `0` | Vector dimensions |
| `distance_metric` | string | `"cosine"` | `"cosine"`, `"l2"`, or `"ip"` |
| `datatype` | string | `"FLOAT32"` | Vector data type |
| `m` | int | `16` | HNSW: max edges per node |
| `ef_construction` | int | `200` | HNSW: construction search depth |
| `ef_runtime` | int | `10` | HNSW: query search depth |
| `block_size` | int | `1024` | FLAT: block size |

## Schema Utilities

```csharp
// Get key prefix (with trailing colon)
string prefix = schema.GetKeyPrefix(); // "product:"

// Find a field by name
var field = schema.GetField("title"); // TextField

// Get the vector field
var vectorField = schema.GetVectorField(); // VectorField or null
```

## JSON Storage with Field Paths

When using JSON storage, fields use `$.fieldName` paths automatically:

```csharp
var field = new TextField { Name = "title" };
field.FieldPath; // "$.title"

// Override with explicit path
var nested = new TextField { Name = "title", Path = "$.metadata.title" };
nested.FieldPath; // "$.metadata.title"
```


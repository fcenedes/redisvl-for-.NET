using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using RedisVL.Schema.Fields;
using RedisVL.Exceptions;

namespace RedisVL.Schema;

/// <summary>
/// Index configuration including name, prefix, and storage type.
/// </summary>
public class IndexInfo
{
    [JsonPropertyName("name")]
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("prefix")]
    [YamlMember(Alias = "prefix")]
    public string Prefix { get; set; } = string.Empty;
    
    [JsonPropertyName("storage_type")]
    [YamlMember(Alias = "storage_type")]
    public string StorageTypeString { get; set; } = "hash";
    
    [JsonIgnore]
    [YamlIgnore]
    public StorageType StorageType => StorageTypeString.ToLowerInvariant() switch
    {
        "json" => StorageType.Json,
        _ => StorageType.Hash
    };
}

/// <summary>
/// Raw field definition from YAML/JSON.
/// </summary>
public class FieldDefinition
{
    [JsonPropertyName("name")]
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("path")]
    [YamlMember(Alias = "path")]
    public string? Path { get; set; }
    
    [JsonPropertyName("attrs")]
    [YamlMember(Alias = "attrs")]
    public Dictionary<string, object>? Attrs { get; set; }
}

/// <summary>
/// Raw schema definition from YAML/JSON.
/// </summary>
public class SchemaDefinition
{
    [JsonPropertyName("index")]
    [YamlMember(Alias = "index")]
    public IndexInfo Index { get; set; } = new();
    
    [JsonPropertyName("fields")]
    [YamlMember(Alias = "fields")]
    public List<FieldDefinition> Fields { get; set; } = new();
}

/// <summary>
/// Defines the schema for a Redis search index.
/// </summary>
public class IndexSchema
{
    public IndexInfo Index { get; private set; } = new();
    public List<FieldBase> Fields { get; private set; } = new();
    
    /// <summary>
    /// Creates an IndexSchema from a YAML file.
    /// </summary>
    public static IndexSchema FromYaml(string yamlPath)
    {
        var yaml = File.ReadAllText(yamlPath);
        return FromYamlString(yaml);
    }
    
    /// <summary>
    /// Creates an IndexSchema from a YAML string.
    /// </summary>
    public static IndexSchema FromYamlString(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        
        var definition = deserializer.Deserialize<SchemaDefinition>(yaml);
        return FromDefinition(definition);
    }
    
    /// <summary>
    /// Creates an IndexSchema from a JSON string.
    /// </summary>
    public static IndexSchema FromJson(string json)
    {
        var definition = JsonSerializer.Deserialize<SchemaDefinition>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new SchemaValidationException("Failed to parse JSON schema");
        
        return FromDefinition(definition);
    }
    
    /// <summary>
    /// Creates an IndexSchema from a dictionary.
    /// </summary>
    public static IndexSchema FromDictionary(Dictionary<string, object> dict)
    {
        var json = JsonSerializer.Serialize(dict);
        return FromJson(json);
    }
    
    private static IndexSchema FromDefinition(SchemaDefinition definition)
    {
        var schema = new IndexSchema
        {
            Index = definition.Index
        };
        
        foreach (var fieldDef in definition.Fields)
        {
            schema.Fields.Add(CreateField(fieldDef));
        }
        
        return schema;
    }
    
    private static FieldBase CreateField(FieldDefinition def)
    {
        return def.Type.ToLowerInvariant() switch
        {
            "text" => CreateTextField(def),
            "tag" => CreateTagField(def),
            "numeric" => CreateNumericField(def),
            "geo" => CreateGeoField(def),
            "vector" => CreateVectorField(def),
            _ => throw new SchemaValidationException($"Unknown field type: {def.Type}")
        };
    }
    
    // Helper to safely convert values that may be JsonElement or native types
    private static double ToDouble(object value)
    {
        if (value is JsonElement je) return je.GetDouble();
        return Convert.ToDouble(value);
    }
    
    private static int ToInt32(object value)
    {
        if (value is JsonElement je) return je.GetInt32();
        return Convert.ToInt32(value);
    }
    
    private static bool ToBool(object value)
    {
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.True) return true;
            if (je.ValueKind == JsonValueKind.False) return false;
            return bool.Parse(je.GetString()!);
        }
        return Convert.ToBoolean(value);
    }
    
    private static string? ToStr(object? value)
    {
        if (value is JsonElement je) return je.GetString();
        return value?.ToString();
    }
    
    private static TextField CreateTextField(FieldDefinition def)
    {
        var field = new TextField { Name = def.Name, Path = def.Path };
        if (def.Attrs != null)
        {
            if (def.Attrs.TryGetValue("weight", out var weight))
                field.Weight = ToDouble(weight);
            if (def.Attrs.TryGetValue("no_stem", out var noStem))
                field.NoStem = ToBool(noStem);
            if (def.Attrs.TryGetValue("sortable", out var sortable))
                field.Sortable = ToBool(sortable);
            if (def.Attrs.TryGetValue("no_index", out var noIndex))
                field.NoIndex = ToBool(noIndex);
        }
        return field;
    }
    
    private static TagField CreateTagField(FieldDefinition def)
    {
        var field = new TagField { Name = def.Name, Path = def.Path };
        if (def.Attrs != null)
        {
            if (def.Attrs.TryGetValue("separator", out var separator))
                field.Separator = ToStr(separator) ?? ",";
            if (def.Attrs.TryGetValue("case_sensitive", out var caseSensitive))
                field.CaseSensitive = ToBool(caseSensitive);
            if (def.Attrs.TryGetValue("sortable", out var sortable))
                field.Sortable = ToBool(sortable);
        }
        return field;
    }
    
    private static NumericField CreateNumericField(FieldDefinition def)
    {
        var field = new NumericField { Name = def.Name, Path = def.Path };
        if (def.Attrs != null)
        {
            if (def.Attrs.TryGetValue("sortable", out var sortable))
                field.Sortable = ToBool(sortable);
            if (def.Attrs.TryGetValue("no_index", out var noIndex))
                field.NoIndex = ToBool(noIndex);
        }
        return field;
    }
    
    private static GeoField CreateGeoField(FieldDefinition def)
    {
        var field = new GeoField { Name = def.Name, Path = def.Path };
        if (def.Attrs != null)
        {
            if (def.Attrs.TryGetValue("sortable", out var sortable))
                field.Sortable = ToBool(sortable);
        }
        return field;
    }
    
    private static VectorField CreateVectorField(FieldDefinition def)
    {
        var field = new VectorField { Name = def.Name, Path = def.Path };
        if (def.Attrs != null)
        {
            if (def.Attrs.TryGetValue("dims", out var dims))
                field.Dims = ToInt32(dims);
            if (def.Attrs.TryGetValue("algorithm", out var algorithm))
            {
                field.Algorithm = ToStr(algorithm)?.ToLowerInvariant() switch
                {
                    "flat" => VectorAlgorithm.Flat,
                    "hnsw" => VectorAlgorithm.HNSW,
                    _ => VectorAlgorithm.HNSW
                };
            }
            if (def.Attrs.TryGetValue("distance_metric", out var metric))
            {
                field.DistanceMetric = ToStr(metric)?.ToLowerInvariant() switch
                {
                    "cosine" => DistanceMetric.Cosine,
                    "l2" => DistanceMetric.L2,
                    "ip" => DistanceMetric.IP,
                    _ => DistanceMetric.Cosine
                };
            }
            if (def.Attrs.TryGetValue("datatype", out var datatype))
                field.DataType = ToStr(datatype) ?? "FLOAT32";
            if (def.Attrs.TryGetValue("m", out var m))
                field.M = ToInt32(m);
            if (def.Attrs.TryGetValue("ef_construction", out var efConstruction))
                field.EfConstruction = ToInt32(efConstruction);
            if (def.Attrs.TryGetValue("ef_runtime", out var efRuntime))
                field.EfRuntime = ToInt32(efRuntime);
        }
        return field;
    }
    
    /// <summary>
    /// Gets the key prefix with colon if needed.
    /// </summary>
    public string GetKeyPrefix() => Index.Prefix.EndsWith(":") ? Index.Prefix : $"{Index.Prefix}:";
    
    /// <summary>
    /// Gets a specific field by name.
    /// </summary>
    public FieldBase? GetField(string name) => Fields.FirstOrDefault(f => f.Name == name);
    
    /// <summary>
    /// Gets the vector field if one exists.
    /// </summary>
    public VectorField? GetVectorField() => Fields.OfType<VectorField>().FirstOrDefault();
}

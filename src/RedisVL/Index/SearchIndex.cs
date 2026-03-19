using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;
using RedisVL.Exceptions;
using RedisVL.Query;
using RedisVL.Schema;
using RedisVL.Schema.Fields;

namespace RedisVL.Index;

/// <summary>
/// Redis search index implementation supporting Hash and JSON storage.
/// </summary>
public class SearchIndex : ISearchIndex, IDisposable
{
    private readonly RedisConnectionProvider? _provider;
    private readonly bool _ownsProvider;
    private readonly IDatabase _db;
    
    /// <inheritdoc />
    public IndexSchema Schema { get; }
    
    /// <inheritdoc />
    public string Name => Schema.Index.Name;
    
    /// <summary>
    /// Creates a SearchIndex with a schema and Redis URL.
    /// </summary>
    public SearchIndex(IndexSchema schema, string redisUrl)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _provider = new RedisConnectionProvider(redisUrl);
        _ownsProvider = true;
        _db = _provider.GetDatabase();
    }
    
    /// <summary>
    /// Creates a SearchIndex with a schema and an existing connection provider.
    /// </summary>
    public SearchIndex(IndexSchema schema, RedisConnectionProvider provider)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _ownsProvider = false;
        _db = _provider.GetDatabase();
    }
    
    /// <summary>
    /// Creates a SearchIndex with a schema and an existing IDatabase.
    /// </summary>
    public SearchIndex(IndexSchema schema, IDatabase database)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _db = database ?? throw new ArgumentNullException(nameof(database));
        _provider = null;
        _ownsProvider = false;
    }
    
    /// <inheritdoc />
    public async Task CreateAsync(bool overwrite = false)
    {
        try
        {
            if (overwrite && await ExistsAsync())
            {
                await DropAsync(false);
            }
            
            var ft = _db.FT();
            var redisSchema = BuildRedisSchema();
            var createParams = new FTCreateParams()
                .On(Schema.Index.StorageType == StorageType.Json ? IndexDataType.JSON : IndexDataType.HASH)
                .Prefix(Schema.GetKeyPrefix());
            
            await ft.CreateAsync(Name, createParams, redisSchema);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            // Index already exists, ignore if not overwriting
        }
        catch (Exception ex) when (ex is not IndexException)
        {
            throw new IndexException($"Failed to create index '{Name}'.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task DropAsync(bool dropDocuments = false)
    {
        try
        {
            var ft = _db.FT();
            await ft.DropIndexAsync(Name, dd: dropDocuments);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index name") || 
                                                ex.Message.Contains("Unknown Index name"))
        {
            // Index doesn't exist, ignore
        }
        catch (Exception ex) when (ex is not IndexException)
        {
            throw new IndexException($"Failed to drop index '{Name}'.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> ExistsAsync()
    {
        try
        {
            var ft = _db.FT();
            await ft.InfoAsync(Name);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<string, object>> InfoAsync()
    {
        try
        {
            var ft = _db.FT();
            var info = await ft.InfoAsync(Name);
            
            var result = new Dictionary<string, object>
            {
                ["index_name"] = info.IndexName,
                ["num_docs"] = info.NumDocs,
                ["num_records"] = info.NumRecords,
                ["max_doc_id"] = info.MaxDocId
            };
            
            return result;
        }
        catch (Exception ex)
        {
            throw new IndexException($"Failed to get info for index '{Name}'.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task LoadAsync(IEnumerable<Dictionary<string, object>> data, string? idField = null,
        IEnumerable<string>? keys = null, TimeSpan? ttl = null)
    {
        var dataList = data.ToList();
        var keyList = keys?.ToList();
        
        if (keyList != null && keyList.Count != dataList.Count)
            throw new ArgumentException("Number of keys must match number of data items.");
        
        var isJson = Schema.Index.StorageType == StorageType.Json;
        var prefix = Schema.GetKeyPrefix();
        var tasks = new List<Task>();
        
        for (int i = 0; i < dataList.Count; i++)
        {
            var doc = dataList[i];
            string key;
            
            if (keyList != null)
            {
                key = keyList[i].StartsWith(prefix) ? keyList[i] : $"{prefix}{keyList[i]}";
            }
            else if (idField != null && doc.TryGetValue(idField, out var idValue))
            {
                key = $"{prefix}{idValue}";
            }
            else
            {
                key = $"{prefix}{Guid.NewGuid()}";
            }
            
            if (isJson)
            {
                var json = _db.JSON();
                var jsonStr = System.Text.Json.JsonSerializer.Serialize(doc);
                tasks.Add(json.SetAsync(key, "$", jsonStr));
            }
            else
            {
                var entries = doc.Select(kvp => new HashEntry(
                    kvp.Key, 
                    ConvertToRedisValue(kvp.Value)
                )).ToArray();
                
                tasks.Add(_db.HashSetAsync(key, entries));
            }
            
            if (ttl.HasValue)
            {
                tasks.Add(_db.KeyExpireAsync(key, ttl.Value));
            }
        }
        
        await Task.WhenAll(tasks);
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<string, object>?> FetchAsync(string id)
    {
        var prefix = Schema.GetKeyPrefix();
        var key = id.StartsWith(prefix) ? id : $"{prefix}{id}";
        
        if (Schema.Index.StorageType == StorageType.Json)
        {
            var json = _db.JSON();
            var result = await json.GetAsync(key);
            if (result.IsNull)
                return null;
            
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(result.ToString()!);
        }
        else
        {
            var entries = await _db.HashGetAllAsync(key);
            if (entries.Length == 0)
                return null;
            
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => (object)e.Value.ToString()!
            );
        }
    }
    
    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<string> ids)
    {
        var prefix = Schema.GetKeyPrefix();
        var redisKeys = ids.Select(id => 
            (RedisKey)(id.StartsWith(prefix) ? id : $"{prefix}{id}")
        ).ToArray();
        
        await _db.KeyDeleteAsync(redisKeys);
    }
    
    /// <inheritdoc />
    public async Task<Query.SearchResults> QueryAsync(BaseQuery query)
    {
        try
        {
            var ft = _db.FT();
            var queryString = query.GetQueryString();
            var redisQuery = new NRedisStack.Search.Query(queryString)
                .Dialect(query.Dialect)
                .Limit(query.Offset, query.NumResults);
            
            if (query.ReturnFields != null && query.ReturnFields.Length > 0)
            {
                redisQuery.ReturnFields(query.ReturnFields);
            }
            
            if (query is VectorQuery vq)
            {
                redisQuery.AddParam("vec_param", vq.GetVectorBytes());
                redisQuery.SetSortBy(vq.ScoreFieldName);
            }
            else if (query is RangeQuery rq)
            {
                redisQuery.AddParam("vec_param", rq.GetVectorBytes());
            }
            else if (query is HybridQuery hq)
            {
                redisQuery.AddParam("vec_param", hq.GetVectorBytes());
            }
            else if (query is FilterQuery fq && fq.SortBy != null)
            {
                redisQuery.SetSortBy(fq.SortBy, fq.SortAscending);
            }
            else if (query is TextQuery tq && tq.SortBy != null)
            {
                redisQuery.SetSortBy(tq.SortBy, tq.SortAscending);
            }
            
            var searchResult = await ft.SearchAsync(Name, redisQuery);
            
            return ConvertSearchResult(searchResult);
        }
        catch (Exception ex) when (ex is not IndexException)
        {
            throw new IndexException($"Query failed on index '{Name}'.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<long> CountAsync(CountQuery? query = null)
    {
        try
        {
            var ft = _db.FT();
            var queryString = query?.GetQueryString() ?? "*";
            var redisQuery = new NRedisStack.Search.Query(queryString)
                .Dialect(query?.Dialect ?? 2)
                .Limit(0, 0);
            
            var result = await ft.SearchAsync(Name, redisQuery);
            return result.TotalResults;
        }
        catch (Exception ex) when (ex is not IndexException)
        {
            throw new IndexException($"Count query failed on index '{Name}'.", ex);
        }
    }
    
    private NRedisStack.Search.Schema BuildRedisSchema()
    {
        var schema = new NRedisStack.Search.Schema();
        var isJson = Schema.Index.StorageType == StorageType.Json;
        
        foreach (var field in Schema.Fields)
        {
            var fieldName = isJson ? field.FieldPath : field.Name;
            var alias = isJson ? field.Name : null;
            var fn = alias != null ? new FieldName(fieldName, alias) : FieldName.Of(fieldName);
            
            switch (field)
            {
                case Schema.Fields.TextField tf:
                    schema.AddTextField(fn, weight: tf.Weight, sortable: tf.Sortable,
                        noStem: tf.NoStem, phonetic: tf.Phonetic, noIndex: tf.NoIndex);
                    break;
                    
                case Schema.Fields.TagField tgf:
                    schema.AddTagField(fn, sortable: tgf.Sortable, noIndex: tgf.NoIndex,
                        separator: tgf.Separator, caseSensitive: tgf.CaseSensitive);
                    break;
                    
                case Schema.Fields.NumericField nf:
                    schema.AddNumericField(fn, sortable: nf.Sortable, noIndex: nf.NoIndex);
                    break;
                    
                case Schema.Fields.GeoField gf:
                    schema.AddGeoField(fn, sortable: gf.Sortable, noIndex: gf.NoIndex);
                    break;
                    
                case Schema.Fields.VectorField vf:
                    var algo = vf.Algorithm == VectorAlgorithm.Flat 
                        ? NRedisStack.Search.Schema.VectorField.VectorAlgo.FLAT 
                        : NRedisStack.Search.Schema.VectorField.VectorAlgo.HNSW;
                    var attrs = new Dictionary<string, object>
                    {
                        ["TYPE"] = vf.DataType.ToUpperInvariant(),
                        ["DIM"] = vf.Dims.ToString(),
                        ["DISTANCE_METRIC"] = vf.GetDistanceMetricString()
                    };
                    
                    if (vf.Algorithm == VectorAlgorithm.HNSW)
                    {
                        attrs["M"] = vf.M.ToString();
                        attrs["EF_CONSTRUCTION"] = vf.EfConstruction.ToString();
                        attrs["EF_RUNTIME"] = vf.EfRuntime.ToString();
                    }
                    else
                    {
                        attrs["BLOCK_SIZE"] = vf.BlockSize.ToString();
                    }
                    
                    schema.AddVectorField(fn, algo, attrs);
                    break;
            }
        }
        
        return schema;
    }
    
    private static Query.SearchResults ConvertSearchResult(NRedisStack.Search.SearchResult searchResult)
    {
        var results = new Query.SearchResults
        {
            TotalResults = searchResult.TotalResults,
            Documents = new List<Query.SearchResult>()
        };
        
        foreach (var doc in searchResult.Documents)
        {
            var result = new Query.SearchResult
            {
                Id = doc.Id
            };
            
            foreach (var prop in doc.GetProperties())
            {
                var key = prop.Key;
                var value = prop.Value;
                
                if (key is "vector_distance" or "hybrid_score" or "score")
                {
                    if (double.TryParse(value.ToString(), out var score))
                    {
                        result.Score = score;
                    }
                }
                
                result.Fields[key] = value.ToString();
            }
            
            results.Documents.Add(result);
        }
        
        return results;
    }
    
    private static RedisValue ConvertToRedisValue(object value)
    {
        return value switch
        {
            string s => s,
            int i => i,
            long l => l,
            double d => d,
            float f => f,
            bool b => b.ToString(),
            byte[] bytes => bytes,
            float[] floats => FloatArrayToBytes(floats),
            _ => value.ToString() ?? string.Empty
        };
    }
    
    private static byte[] FloatArrayToBytes(float[] floats)
    {
        var bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }
    
    public void Dispose()
    {
        if (_ownsProvider)
        {
            _provider?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

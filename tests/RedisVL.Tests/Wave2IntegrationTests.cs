using RedisVL.Extensions.Cache;
using RedisVL.Extensions.MessageHistory;
using RedisVL.Extensions.Router;
using RedisVL.Index;
using RedisVL.Query;
using RedisVL.Query.Filter;
using RedisVL.Schema;
using RedisVL.Schema.Fields;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Tests;

/// <summary>
/// Wave 2: Integration tests — requires Redis on localhost:6379.
/// </summary>
public static class TestHelpers
{
    private const int Dims = 8;

    public static CustomTextVectorizer CreateFakeVectorizer()
    {
        return new CustomTextVectorizer(
            embedFunc: text =>
            {
                var embedding = new float[Dims];
                var hash = text.GetHashCode();
                for (int i = 0; i < Dims; i++)
                    embedding[i] = ((hash >> i) & 1) == 1 ? 1.0f : -1.0f;
                var mag = (float)Math.Sqrt(embedding.Sum(x => x * x));
                for (int i = 0; i < Dims; i++) embedding[i] /= mag;
                return Task.FromResult(embedding);
            },
            dims: Dims,
            model: "fake-test");
    }

    public static string UniqueIndexName(string prefix = "test")
        => $"{prefix}-{Guid.NewGuid():N}"[..24];
}

[Trait("Category", "Integration")]
public class SearchIndexIntegrationTests : IAsyncLifetime
{
    private SearchIndex _index = null!;
    private string _indexName = null!;

    public async Task InitializeAsync()
    {
        _indexName = TestHelpers.UniqueIndexName("si");
        var json = $@"{{
            ""index"": {{ ""name"": ""{_indexName}"", ""prefix"": ""{_indexName}"", ""storage_type"": ""hash"" }},
            ""fields"": [
                {{ ""name"": ""title"", ""type"": ""text"", ""attrs"": {{ ""sortable"": true }} }},
                {{ ""name"": ""category"", ""type"": ""tag"" }},
                {{ ""name"": ""price"", ""type"": ""numeric"", ""attrs"": {{ ""sortable"": true }} }},
                {{ ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": {{ ""algorithm"": ""flat"", ""dims"": 8, ""distance_metric"": ""cosine"" }} }}
            ]
        }}";
        _index = new SearchIndex(IndexSchema.FromJson(json), "redis://localhost:6379");
        await _index.CreateAsync(overwrite: true);
    }

    public async Task DisposeAsync()
    {
        await _index.DropAsync(dropDocuments: true);
        _index.Dispose();
    }

    [Fact]
    public async Task CreateAndExists_IndexIsCreated()
    {
        Assert.True(await _index.ExistsAsync());
    }

    [Fact]
    public async Task DropAndExists_IndexIsGone()
    {
        var name2 = TestHelpers.UniqueIndexName("drop");
        var json = $@"{{
            ""index"": {{ ""name"": ""{name2}"", ""prefix"": ""{name2}"" }},
            ""fields"": [{{ ""name"": ""t"", ""type"": ""text"" }}]
        }}";
        using var idx2 = new SearchIndex(IndexSchema.FromJson(json), "redis://localhost:6379");
        await idx2.CreateAsync();
        Assert.True(await idx2.ExistsAsync());
        await idx2.DropAsync();
        Assert.False(await idx2.ExistsAsync());
    }

    [Fact]
    public async Task LoadAndFetch_RoundTrips()
    {
        var data = new[] { new Dictionary<string, object>
        {
            ["id"] = "item1", ["title"] = "Redis Book", ["category"] = "tech",
            ["price"] = 29.99, ["embedding"] = new float[] { 1, 0, 0, 0, 0, 0, 0, 0 }
        }};
        await _index.LoadAsync(data, idField: "id");
        var fetched = await _index.FetchAsync("item1");
        Assert.NotNull(fetched);
        Assert.Equal("Redis Book", fetched!["title"]);
    }

    [Fact]
    public async Task Delete_RemovesDocument()
    {
        var data = new[] { new Dictionary<string, object>
        {
            ["id"] = "del1", ["title"] = "To Delete", ["category"] = "temp",
            ["price"] = 0, ["embedding"] = new float[] { 0, 1, 0, 0, 0, 0, 0, 0 }
        }};
        await _index.LoadAsync(data, idField: "id");
        Assert.NotNull(await _index.FetchAsync("del1"));
        await _index.DeleteAsync(new[] { "del1" });
        Assert.Null(await _index.FetchAsync("del1"));
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        var data = Enumerable.Range(0, 3).Select(i => new Dictionary<string, object>
        {
            ["id"] = $"cnt{i}", ["title"] = $"Item {i}", ["category"] = "books",
            ["price"] = i * 10, ["embedding"] = new float[] { i, 0, 0, 0, 0, 0, 0, 0 }
        }).ToList();
        await _index.LoadAsync(data, idField: "id");
        await Task.Delay(500);
        var count = await _index.CountAsync();
        Assert.True(count >= 3, $"Expected >=3, got {count}");
    }

    [Fact]
    public async Task QueryAsync_FilterQuery_ReturnsDocs()
    {
        var data = new[] { new Dictionary<string, object>
        {
            ["id"] = "fq1", ["title"] = "Filter Test", ["category"] = "querytest",
            ["price"] = 50, ["embedding"] = new float[] { 1, 1, 0, 0, 0, 0, 0, 0 }
        }};
        await _index.LoadAsync(data, idField: "id");
        await Task.Delay(500);
        var results = await _index.QueryAsync(new FilterQuery
        {
            FilterExpression = Tag.Field("category") == "querytest",
            ReturnFields = new[] { "title", "price" }
        });
        Assert.True(results.TotalResults >= 1);
        Assert.Contains(results.Documents, d => d.GetField<string>("title") == "Filter Test");
    }


    [Fact]
    public async Task QueryAsync_VectorQuery_ReturnsSortedByDistance()
    {
        var data = new[]
        {
            new Dictionary<string, object>
            {
                ["id"] = "vq1", ["title"] = "Close", ["category"] = "vectest",
                ["price"] = 10, ["embedding"] = new float[] { 1, 0, 0, 0, 0, 0, 0, 0 }
            },
            new Dictionary<string, object>
            {
                ["id"] = "vq2", ["title"] = "Far", ["category"] = "vectest",
                ["price"] = 20, ["embedding"] = new float[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            }
        };
        await _index.LoadAsync(data, idField: "id");
        await Task.Delay(500);
        var results = await _index.QueryAsync(new VectorQuery(
            new float[] { 1, 0, 0, 0, 0, 0, 0, 0 }, "embedding", 10)
        {
            FilterExpression = Tag.Field("category") == "vectest",
            ReturnFields = new[] { "title" }
        });
        Assert.True(results.Documents.Count >= 2);
        Assert.Equal("Close", results.Documents[0].GetField<string>("title"));
    }

    [Fact]
    public async Task InfoAsync_ReturnsIndexInfo()
    {
        var info = await _index.InfoAsync();
        Assert.Equal(_indexName, info["index_name"]);
    }

    [Fact]
    public async Task LoadAsync_WithExplicitKeys_UsesKeys()
    {
        var data = new[] { new Dictionary<string, object>
        {
            ["title"] = "Keyed", ["category"] = "keytest",
            ["price"] = 0, ["embedding"] = new float[] { 0, 0, 1, 0, 0, 0, 0, 0 }
        }};
        await _index.LoadAsync(data, keys: new[] { "mykey1" });
        var fetched = await _index.FetchAsync("mykey1");
        Assert.NotNull(fetched);
        Assert.Equal("Keyed", fetched!["title"]);
    }
}

// ── SemanticCache Integration ──

[Trait("Category", "Integration")]
public class SemanticCacheIntegrationTests : IAsyncLifetime
{
    private SemanticCache _cache = null!;

    public Task InitializeAsync()
    {
        var vectorizer = TestHelpers.CreateFakeVectorizer();
        _cache = new SemanticCache(
            TestHelpers.UniqueIndexName("sc"),
            vectorizer,
            "redis://localhost:6379",
            distanceThreshold: 0.5);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _cache.ClearAsync();
        _cache.Dispose();
    }

    [Fact]
    public async Task StoreAndCheck_ExactMatch_ReturnsCachedResponse()
    {
        await _cache.StoreAsync("What is Redis?", "Redis is an in-memory database.");
        await Task.Delay(500);
        var results = await _cache.CheckAsync("What is Redis?");
        Assert.NotEmpty(results);
        Assert.Equal("Redis is an in-memory database.", results[0].Response);
    }

    [Fact]
    public async Task Check_NoMatch_ReturnsEmpty()
    {
        var results = await _cache.CheckAsync("completely unrelated query xyz123");
        Assert.Empty(results);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        await _cache.StoreAsync("test prompt", "test response");
        await _cache.ClearAsync();
        await Task.Delay(300);
        // After clear, re-init and check should return nothing
        var results = await _cache.CheckAsync("test prompt");
        Assert.Empty(results);
    }

    [Fact]
    public async Task StoreAsync_WithMetadata_MetadataPreserved()
    {
        var metadata = new Dictionary<string, string> { ["model"] = "gpt-4", ["tokens"] = "50" };
        await _cache.StoreAsync("meta test", "meta response", metadata);
        await Task.Delay(500);
        var results = await _cache.CheckAsync("meta test");
        Assert.NotEmpty(results);
        Assert.NotNull(results[0].Metadata);
        Assert.Equal("gpt-4", results[0].Metadata!["model"]);
    }
}

// ── EmbeddingsCache Integration ──

[Trait("Category", "Integration")]
public class EmbeddingsCacheIntegrationTests : IAsyncLifetime
{
    private EmbeddingsCache _cache = null!;

    public Task InitializeAsync()
    {
        _cache = new EmbeddingsCache(
            "redis://localhost:6379",
            prefix: TestHelpers.UniqueIndexName("ec"));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _cache.ClearAsync();
        _cache.Dispose();
    }

    [Fact]
    public async Task SetAndGet_RoundTrips()
    {
        var emb = new float[] { 1f, 2f, 3f };
        await _cache.SetAsync("hello", "model-a", emb);
        var entry = await _cache.GetAsync("hello", "model-a");
        Assert.NotNull(entry);
        Assert.Equal("hello", entry!.Text);
        Assert.Equal("model-a", entry.ModelName);
        Assert.Equal(emb, entry.Embedding);
    }

    [Fact]
    public async Task ExistsAsync_TrueWhenPresent()
    {
        await _cache.SetAsync("exists-test", "m", new float[] { 1f });
        Assert.True(await _cache.ExistsAsync("exists-test", "m"));
    }

    [Fact]
    public async Task ExistsAsync_FalseWhenAbsent()
    {
        Assert.False(await _cache.ExistsAsync("no-such-key", "m"));
    }

    [Fact]
    public async Task DropAsync_RemovesEntry()
    {
        await _cache.SetAsync("drop-me", "m", new float[] { 1f });
        Assert.True(await _cache.ExistsAsync("drop-me", "m"));
        await _cache.DropAsync("drop-me", "m");
        Assert.False(await _cache.ExistsAsync("drop-me", "m"));
    }

    [Fact]
    public async Task MSetAndMGet_BatchOperations()
    {
        var texts = new List<string> { "batch1", "batch2" };
        var embeddings = new List<float[]> { new[] { 1f, 2f }, new[] { 3f, 4f } };
        await _cache.MSetAsync(texts, "bmodel", embeddings);

        var results = await _cache.MGetAsync(texts, "bmodel");
        Assert.Equal(2, results.Count);
        Assert.Equal("batch1", results[0]!.Text);
        Assert.Equal("batch2", results[1]!.Text);
    }

    [Fact]
    public async Task SetAsync_WithMetadata_Preserved()
    {
        var meta = new Dictionary<string, string> { ["source"] = "test" };
        await _cache.SetAsync("meta-emb", "m", new float[] { 1f }, meta);
        var entry = await _cache.GetAsync("meta-emb", "m");
        Assert.NotNull(entry?.Metadata);
        Assert.Equal("test", entry!.Metadata!["source"]);
    }
}


// ── BaseMessageHistory Integration ──

[Trait("Category", "Integration")]
public class BaseMessageHistoryIntegrationTests : IAsyncLifetime
{
    private BaseMessageHistory _history = null!;

    public Task InitializeAsync()
    {
        _history = new BaseMessageHistory(
            TestHelpers.UniqueIndexName("mh"),
            "redis://localhost:6379");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _history.ClearAsync();
        _history.Dispose();
    }

    [Fact]
    public async Task AddAndGetRecent_ReturnsMessages()
    {
        await _history.AddMessagesAsync(new[]
        {
            new Message { Role = "user", Content = "Hello" },
            new Message { Role = "llm", Content = "Hi there!" }
        });
        await Task.Delay(500);
        var recent = await _history.GetRecentAsync(topK: 10);
        Assert.True(recent.Count >= 2);
        Assert.Contains(recent, m => m.Content == "Hello");
        Assert.Contains(recent, m => m.Content == "Hi there!");
    }

    [Fact]
    public async Task GetRecentAsync_WithRoleFilter_FiltersCorrectly()
    {
        await _history.AddMessagesAsync(new[]
        {
            new Message { Role = "user", Content = "Question" },
            new Message { Role = "llm", Content = "Answer" },
            new Message { Role = "system", Content = "System msg" }
        });
        await Task.Delay(500);
        var userOnly = await _history.GetRecentAsync(topK: 10, role: "user");
        Assert.All(userOnly, m => Assert.Equal("user", m.Role));
        Assert.True(userOnly.Count >= 1);
    }

    [Fact]
    public async Task CountAsync_ReturnsMessageCount()
    {
        await _history.AddMessageAsync(new Message { Role = "user", Content = "One" });
        await _history.AddMessageAsync(new Message { Role = "llm", Content = "Two" });
        await Task.Delay(500);
        var count = await _history.CountAsync();
        Assert.True(count >= 2);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllMessages()
    {
        await _history.AddMessageAsync(new Message { Role = "user", Content = "temp" });
        await _history.ClearAsync();
        await Task.Delay(300);
        // After clear, count may throw or return 0. Re-add to verify clean state.
        await _history.AddMessageAsync(new Message { Role = "user", Content = "fresh" });
        await Task.Delay(500);
        var count = await _history.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddMessageAsync_WithMetadata_Preserved()
    {
        var meta = new Dictionary<string, string> { ["model"] = "gpt-4" };
        await _history.AddMessageAsync(new Message
        {
            Role = "llm", Content = "Response", Metadata = meta
        });
        await Task.Delay(500);
        var recent = await _history.GetRecentAsync(topK: 1);
        Assert.NotEmpty(recent);
        Assert.NotNull(recent[0].Metadata);
        Assert.Equal("gpt-4", recent[0].Metadata!["model"]);
    }
}

// ── SemanticMessageHistory Integration ──

[Trait("Category", "Integration")]
public class SemanticMessageHistoryIntegrationTests : IAsyncLifetime
{
    private SemanticMessageHistory _history = null!;

    public Task InitializeAsync()
    {
        _history = new SemanticMessageHistory(
            TestHelpers.UniqueIndexName("smh"),
            TestHelpers.CreateFakeVectorizer(),
            "redis://localhost:6379",
            distanceThreshold: 0.9);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _history.ClearAsync();
        _history.Dispose();
    }

    [Fact]
    public async Task AddAndGetRecent_Works()
    {
        await _history.AddMessagesAsync(new[]
        {
            new Message { Role = "user", Content = "Tell me about Redis" },
            new Message { Role = "llm", Content = "Redis is a fast database" }
        });
        await Task.Delay(500);
        var recent = await _history.GetRecentAsync(topK: 10);
        Assert.True(recent.Count >= 2);
    }

    [Fact]
    public async Task GetRelevantAsync_ReturnsSemanticallyRelevant()
    {
        await _history.AddMessagesAsync(new[]
        {
            new Message { Role = "user", Content = "Tell me about Redis" },
            new Message { Role = "llm", Content = "Redis is a fast database" },
            new Message { Role = "user", Content = "What is the weather?" }
        });
        await Task.Delay(500);
        // Query same text — should return exact match
        var relevant = await _history.GetRelevantAsync("Tell me about Redis", topK: 5);
        Assert.NotEmpty(relevant);
    }

    [Fact]
    public async Task GetRelevantAsync_WithRoleFilter_FiltersCorrectly()
    {
        await _history.AddMessagesAsync(new[]
        {
            new Message { Role = "user", Content = "User question" },
            new Message { Role = "llm", Content = "LLM answer" }
        });
        await Task.Delay(500);
        var userOnly = await _history.GetRelevantAsync("User question", topK: 10, role: "user");
        Assert.All(userOnly, m => Assert.Equal("user", m.Role));
    }
}

// ── SemanticRouter Integration ──

[Trait("Category", "Integration")]
public class SemanticRouterIntegrationTests : IAsyncLifetime
{
    private SemanticRouter _router = null!;

    public Task InitializeAsync()
    {
        var routes = new List<Route>
        {
            new Route
            {
                Name = "greeting",
                References = new List<string> { "hello", "hi", "hey", "good morning" },
                DistanceThreshold = 0.9
            },
            new Route
            {
                Name = "farewell",
                References = new List<string> { "bye", "goodbye", "see you later" },
                DistanceThreshold = 0.9
            }
        };
        _router = new SemanticRouter(
            TestHelpers.UniqueIndexName("sr"),
            routes,
            TestHelpers.CreateFakeVectorizer(),
            "redis://localhost:6379");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _router.ClearAsync();
        _router.Dispose();
    }

    [Fact]
    public async Task RouteAsync_ExactMatch_ReturnsCorrectRoute()
    {
        // "hello" is a reference for greeting route — exact match should work
        var match = await _router.RouteAsync("hello");
        Assert.NotNull(match);
        Assert.Equal("greeting", match!.Name);
    }

    [Fact]
    public async Task RouteAsync_FarewellMatch_ReturnsCorrectRoute()
    {
        var match = await _router.RouteAsync("goodbye");
        Assert.NotNull(match);
        Assert.Equal("farewell", match!.Name);
    }

    [Fact]
    public async Task RouteAsync_NoMatch_ReturnsNull()
    {
        // Use a very low threshold router for this test
        var routes = new List<Route>
        {
            new Route
            {
                Name = "narrow",
                References = new List<string> { "very specific phrase only" },
                DistanceThreshold = 0.001 // extremely strict
            }
        };
        using var strictRouter = new SemanticRouter(
            TestHelpers.UniqueIndexName("srn"),
            routes,
            TestHelpers.CreateFakeVectorizer(),
            "redis://localhost:6379");

        var match = await strictRouter.RouteAsync("completely different text xyz");
        Assert.Null(match);
        await strictRouter.ClearAsync();
    }

    [Fact]
    public async Task InvokeAsync_SameAsRouteAsync()
    {
        var match = await _router.InvokeAsync("hello");
        Assert.NotNull(match);
        Assert.Equal("greeting", match!.Name);
    }

    [Fact]
    public async Task RouteAsync_ReturnsDistance()
    {
        var match = await _router.RouteAsync("hello");
        Assert.NotNull(match);
        Assert.True(match!.Distance >= 0);
    }
}
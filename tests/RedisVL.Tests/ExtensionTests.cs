using RedisVL.Extensions.Cache;
using RedisVL.Extensions.MessageHistory;
using RedisVL.Extensions.Router;

namespace RedisVL.Tests;

public class SemanticCacheTests
{
    [Fact]
    public void SemanticCache_Constructor_NullVectorizer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SemanticCache("test", null!));
    }

    [Fact]
    public void CacheEntry_Properties_DefaultValues()
    {
        var entry = new CacheEntry();
        Assert.Equal(string.Empty, entry.Prompt);
        Assert.Equal(string.Empty, entry.Response);
        Assert.Null(entry.Metadata);
        Assert.Null(entry.Distance);
    }

    [Fact]
    public void CacheEntry_Properties_CanBeSet()
    {
        var entry = new CacheEntry
        {
            Prompt = "What is Redis?",
            Response = "Redis is an in-memory database.",
            Metadata = new Dictionary<string, string> { ["model"] = "gpt-4" },
            Distance = 0.05
        };

        Assert.Equal("What is Redis?", entry.Prompt);
        Assert.Equal("Redis is an in-memory database.", entry.Response);
        Assert.Equal("gpt-4", entry.Metadata!["model"]);
        Assert.Equal(0.05, entry.Distance);
    }
}

public class ExtendedMessageTests
{
    [Theory]
    [InlineData("user")]
    [InlineData("llm")]
    [InlineData("system")]
    [InlineData("tool")]
    public void Message_AllRoleTypes_AreSupported(string role)
    {
        var msg = new Message
        {
            Role = role,
            Content = "test content"
        };
        Assert.Equal(role, msg.Role);
    }

    [Fact]
    public void Message_WithMetadata_Serialization()
    {
        var msg = new Message
        {
            Role = "llm",
            Content = "response",
            Metadata = new Dictionary<string, string>
            {
                ["model"] = "gpt-4",
                ["tokens"] = "150",
                ["timestamp"] = "2024-01-01"
            }
        };

        Assert.Equal(3, msg.Metadata!.Count);
        Assert.Equal("gpt-4", msg.Metadata["model"]);
        Assert.Equal("150", msg.Metadata["tokens"]);
    }

    [Fact]
    public void Message_DefaultValues_AreCorrect()
    {
        var msg = new Message();
        Assert.Equal(string.Empty, msg.Role);
        Assert.Equal(string.Empty, msg.Content);
        Assert.Null(msg.Metadata);
    }
}

public class ExtendedRouteTests
{
    [Fact]
    public void Route_WithReferences_CanBeSet()
    {
        var route = new Route
        {
            Name = "greeting",
            References = new List<string> { "hello", "hi", "hey", "good morning" }
        };

        Assert.Equal("greeting", route.Name);
        Assert.Equal(4, route.References.Count);
        Assert.Contains("hello", route.References);
    }

    [Fact]
    public void Route_DistanceThreshold_DefaultIs05()
    {
        var route = new Route { Name = "test" };
        Assert.Equal(0.5, route.DistanceThreshold);
    }

    [Fact]
    public void Route_DistanceThreshold_CanBeCustomized()
    {
        var route = new Route
        {
            Name = "strict",
            DistanceThreshold = 0.2
        };
        Assert.Equal(0.2, route.DistanceThreshold);
    }

    [Fact]
    public void RouteMatch_AllProperties_CanBeSet()
    {
        var match = new RouteMatch
        {
            Name = "greeting",
            Distance = 0.15,
            MatchedReference = "hello there",
            Metadata = new Dictionary<string, string> { ["intent"] = "greet" }
        };

        Assert.Equal("greeting", match.Name);
        Assert.Equal(0.15, match.Distance);
        Assert.Equal("hello there", match.MatchedReference);
        Assert.Equal("greet", match.Metadata!["intent"]);
    }

    [Fact]
    public void RouteMatch_DefaultValues_AreCorrect()
    {
        var match = new RouteMatch();
        Assert.Equal(string.Empty, match.Name);
        Assert.Equal(0.0, match.Distance);
        Assert.Null(match.MatchedReference);
        Assert.Null(match.Metadata);
    }

    [Fact]
    public void Route_WithMetadata_CanBeSet()
    {
        var route = new Route
        {
            Name = "support",
            Metadata = new Dictionary<string, string>
            {
                ["handler"] = "SupportAgent",
                ["priority"] = "high"
            }
        };

        Assert.Equal("SupportAgent", route.Metadata!["handler"]);
    }

    [Fact]
    public void SemanticRouter_Constructor_NullRoutes_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SemanticRouter("test", null!, null!));
    }

    [Fact]
    public void SemanticRouter_Constructor_NullVectorizer_Throws()
    {
        var routes = new List<Route> { new Route { Name = "test" } };
        Assert.Throws<ArgumentNullException>(() =>
            new SemanticRouter("test", routes, null!));
    }
}


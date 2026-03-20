using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Extensions.Router;
using RedisVL.Tutorial.Services;

namespace RedisVL.Tutorial.ViewModels;

/// <summary>
/// ViewModel for the Semantic Router demo tab.
/// Classifies user queries into predefined routes using vector similarity.
/// </summary>
public partial class SemanticRouterSectionViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly VectorizerService vectorizerService;

    [Reactive] private string queryText = string.Empty;
    [Reactive] private string output = string.Empty;
    [Reactive] private string matchedRoute = string.Empty;
    [Reactive] private string matchDistance = string.Empty;

    private SemanticRouter? router;

    public SemanticRouterSectionViewModel(VectorizerService vectorizerService)
    {
        this.vectorizerService = vectorizerService;

        Routes = CreateRoutes();

        var canRoute = this.WhenAnyValue(x => x.QueryText)
            .Select(q => !string.IsNullOrWhiteSpace(q));

        Route = ReactiveCommand.CreateFromTask(ExecuteRoute, canRoute);
        disposables.Add(Route);

        Clear = ReactiveCommand.CreateFromTask(ExecuteClear);
        disposables.Add(Clear);

        disposables.Add(
            Route.ThrownExceptions
                .Subscribe(ex => Output = $"Error: {ex.Message}"));

        disposables.Add(
            Clear.ThrownExceptions
                .Subscribe(ex => Output = $"Error: {ex.Message}"));

        disposables.Add(
            vectorizerService.VectorizerChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => RecreateRouter()));
    }

    public string Title => "Semantic Router";

    public IReadOnlyList<RouteInfo> Routes { get; }

    public ReactiveCommand<Unit, Unit> Route { get; }

    public ReactiveCommand<Unit, Unit> Clear { get; }

    private async Task ExecuteRoute()
    {
        EnsureRouter();

        Output = $"Routing query: \"{QueryText}\"...";
        MatchedRoute = string.Empty;
        MatchDistance = string.Empty;

        var match = await router!.RouteAsync(QueryText);

        if (match != null)
        {
            MatchedRoute = match.Name;
            MatchDistance = match.Distance.ToString("F4");
            Output = $"✅ Matched route: \"{match.Name}\"\n" +
                     $"   Distance: {match.Distance:F4}\n" +
                     $"   Closest reference: \"{match.MatchedReference}\"";
        }
        else
        {
            MatchedRoute = "(no match)";
            MatchDistance = "—";
            Output = "❌ No route matched the query.\n" +
                     "The query was not similar enough to any configured route references.";
        }
    }

    private async Task ExecuteClear()
    {
        if (router != null)
        {
            await router.ClearAsync();
            router.Dispose();
            router = null;
        }

        QueryText = string.Empty;
        Output = "Router cleared. It will be re-initialized on the next query.";
        MatchedRoute = string.Empty;
        MatchDistance = string.Empty;
    }

    private void RecreateRouter()
    {
        router?.Dispose();
        router = null;
        Output = "Vectorizer changed — router will be re-initialized on next query.";
        MatchedRoute = string.Empty;
        MatchDistance = string.Empty;
    }

    private void EnsureRouter()
    {
        if (router != null) return;

        router = new SemanticRouter(
            name: "tutorial-router",
            routes: CreateRoutes().ConvertAll(r => r.ToRoute()),
            vectorizer: vectorizerService.CurrentVectorizer);
    }

    private static List<RouteInfo> CreateRoutes()
    {
        return new List<RouteInfo>
        {
            new("greeting",
                new[] { "hello", "hi", "hey", "good morning", "howdy" },
                0.5),
            new("farewell",
                new[] { "bye", "goodbye", "see you later", "take care" },
                0.5),
            new("technical",
                new[] { "how do I", "what is the API", "code example", "documentation" },
                0.7),
            new("general",
                new[] { "tell me about", "what is", "explain" },
                0.6)
        };
    }

    public void Dispose()
    {
        disposables.Dispose();
        router?.Dispose();
    }
}

/// <summary>
/// Display-friendly route information for the view.
/// </summary>
public class RouteInfo
{
    public RouteInfo(string name, IReadOnlyList<string> references, double distanceThreshold)
    {
        Name = name;
        References = references;
        DistanceThreshold = distanceThreshold;
        ReferencesDisplay = string.Join(", ", references);
    }

    public string Name { get; }
    public IReadOnlyList<string> References { get; }
    public double DistanceThreshold { get; }
    public string ReferencesDisplay { get; }

    public Route ToRoute()
    {
        return new Route
        {
            Name = Name,
            References = new List<string>(References),
            DistanceThreshold = DistanceThreshold
        };
    }
}


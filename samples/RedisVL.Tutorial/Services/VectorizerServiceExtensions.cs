using Microsoft.Extensions.DependencyInjection;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial.Services;

/// <summary>
/// Extension methods for registering vectorizer services in the DI container.
/// Called from CompositionRoot (created by Task 1).
/// </summary>
public static class VectorizerServiceExtensions
{
    /// <summary>
    /// Registers the VectorizerService (singleton) and VectorizerConfigViewModel (transient).
    /// </summary>
    public static IServiceCollection AddVectorizerServices(this IServiceCollection services)
    {
        services.AddSingleton<VectorizerService>();
        services.AddTransient<VectorizerConfigViewModel>();
        return services;
    }
}


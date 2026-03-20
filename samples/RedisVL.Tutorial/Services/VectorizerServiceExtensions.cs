using Microsoft.Extensions.DependencyInjection;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial.Services;

/// <summary>
/// Extension methods for registering vectorizer and settings services in the DI container.
/// Called from CompositionRoot.
/// </summary>
public static class VectorizerServiceExtensions
{
    /// <summary>
    /// Registers SettingsService (singleton), VectorizerService (singleton), and SettingsViewModel (transient).
    /// </summary>
    public static IServiceCollection AddVectorizerServices(this IServiceCollection services)
    {
        services.AddSingleton<SettingsService>();
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>().Settings;
            return new VectorizerService(settings);
        });
        services.AddTransient<SettingsViewModel>();
        return services;
    }
}


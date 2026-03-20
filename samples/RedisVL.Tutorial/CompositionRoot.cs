using Microsoft.Extensions.DependencyInjection;
using RedisVL.Tutorial.Services;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial;

public static class CompositionRoot
{
    public static MainWindowViewModel CreateMainViewModel()
    {
        var services = new ServiceCollection();

        services
            .AddVectorizerServices()
            .AddTransient<SemanticCacheSectionViewModel>()
            .AddTransient<EmbeddingsCacheSectionViewModel>()
            .AddTransient<MessageHistorySectionViewModel>()
            .AddTransient<SemanticRouterSectionViewModel>()
            .AddTransient<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<MainWindowViewModel>();
    }
}


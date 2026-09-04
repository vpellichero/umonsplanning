using Microsoft.Extensions.DependencyInjection;

namespace UMonsPlanning.Pronote;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="PronoteClient"/> as a singleton.</summary>
    public static IServiceCollection AddPronoteClient(
        this IServiceCollection services,
        Action<PronoteOptions>? configure = null)
    {
        services.AddOptions<PronoteOptions>()
            .Configure(options => configure?.Invoke(options));

        services.AddSingleton<PronoteClient>();
        services.AddSingleton<IPronoteClient>(sp => sp.GetRequiredService<PronoteClient>());
        return services;
    }
}

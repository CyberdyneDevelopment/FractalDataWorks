using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Etl.Tests.Thresholds;

internal static class WaitThresholdKit
{
    public static WaitThresholdLayer Layer(string owner, string mode) => new(owner, WaitThresholdModes.ByName(mode));

    /// <summary>
    /// The scope factory the singleton gate and reporter open a scope from: the source and the evaluator are scoped, as they are in a host (FDW-4611), and the ambient
    /// accessor is the one singleton every scope shares.
    /// </summary>
    public static IServiceScopeFactory Scopes(IThresholdSource source, IThresholdEvaluator evaluator, IAuthenticationContextAccessor? accessor = null) =>
        Scopes(accessor, services => { services.AddScoped(_ => source); services.AddScoped(_ => evaluator); });

    /// <summary>The same, with a registration of the caller's own for the scoped services.</summary>
    public static IServiceScopeFactory Scopes(IAuthenticationContextAccessor? accessor, Action<IServiceCollection> scoped)
    {
        var services = new ServiceCollection();
        services.AddSingleton(accessor ?? new AuthenticationContextAccessor());
        scoped(services);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}

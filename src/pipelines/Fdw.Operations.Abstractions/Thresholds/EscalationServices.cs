using System;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Resolves the one service a consequence acts through (FDW-3936), and fails loud when the host registered none.</summary>
public static class EscalationServices
{
    /// <summary>Gets a service the host must have registered.</summary>
    /// <typeparam name="TService">The service.</typeparam>
    /// <param name="services">The host services.</param>
    /// <returns>The service; none when the host registered none.</returns>
    public static TService? Find<TService>(IServiceProvider services)
        where TService : class
    {
        return services.GetService(typeof(TService)) as TService;
    }
}

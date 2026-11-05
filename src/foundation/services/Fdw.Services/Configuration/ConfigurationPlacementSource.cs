using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// Names the placement a host registered for the providers that place their members, or says plainly that it registered none (FDW-3692).
/// </summary>
public static class ConfigurationPlacementSource
{
    /// <summary>Gets the placement registered in <paramref name="services"/>, or an <see cref="UnavailablePlacement"/> when there is none.</summary>
    /// <param name="services">The container the provider is being built from.</param>
    /// <returns>A placement that places, or one that refuses every member and says why.</returns>
    public static IConfigurationPlacement From(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.GetService<IConfigurationPlacement>() is { } registered
            ? registered
            : new UnavailablePlacement(services.GetService<ILoggerFactory>()?.CreateLogger<UnavailablePlacement>());
    }
}

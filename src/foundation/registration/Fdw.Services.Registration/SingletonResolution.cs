using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes;

/// <summary>
/// Resolves every singleton a host registers from its built provider, so a captive dependency or a constructor that cannot be filled shows before the host serves a request (FDW-4640).
/// </summary>
/// <remarks>
/// This is what a host's container test calls. The provider must have been built with <see cref="ContainerValidation.Options"/>, so that a singleton which reaches a scoped
/// service through a factory registration is refused when it is resolved. The registration walk in <see cref="ServiceLifetimeCheck"/> cannot see inside a factory; resolving does.
/// </remarks>
public static class SingletonResolution
{
    /// <summary>Resolves every singleton registration.</summary>
    /// <param name="services">The registrations the provider was built from.</param>
    /// <param name="provider">The built provider.</param>
    /// <param name="logger">The logger the findings are written to.</param>
    /// <returns>One coded message for each service type whose singletons could not all be resolved; empty when every one resolved.</returns>
    public static IReadOnlyList<IGenericMessage> ResolveAll(IServiceCollection services, IServiceProvider provider, ILogger logger)
    {
        var findings = new List<IGenericMessage>();
        var singletonTypes = services
            .Where(d => !d.IsKeyedService && d.Lifetime == ServiceLifetime.Singleton && !d.ServiceType.IsGenericTypeDefinition)
            .Select(d => d.ServiceType)
            .Distinct();

        foreach (var serviceType in singletonTypes)
        {
            try
            {
                // Every registration of the type, not only the last one: a sequence of the type resolves them all.
                _ = provider.GetServices(serviceType).ToList();
            }
            catch (Exception exception)
            {
                findings.Add(ServiceTypeLog.SingletonCannotBeResolved(logger, serviceType.ToString(), exception.Message));
            }
        }

        return findings;
    }
}

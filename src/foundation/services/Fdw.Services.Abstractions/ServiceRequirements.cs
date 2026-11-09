using System;
using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Checks, once, that every service a collection and its options declared as required is registered (FDW-4588).
/// </summary>
/// <remarks>
/// <para>
/// It asks the container whether a registration exists (<see cref="IServiceProviderIsService"/>) and never resolves a service: resolving at Initialize depends on the
/// order the domains initialize in and builds services at boot. The check does not see a registered service whose own dependencies are missing.
/// </para>
/// <para>
/// Every missing service is reported, each with its own message, in one failed result, so a host fixes all of them in one pass. A service declared twice is reported once.
/// </para>
/// </remarks>
public static class ServiceRequirements
{
    /// <summary>Checks the requirements against the built host.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="requirements">The declared requirements.</param>
    /// <param name="owner">The name of the collection the requirements belong to, used as the logger category.</param>
    /// <returns>The host, or a failure carrying one message for each service that is not registered.</returns>
    public static IGenericResult<IHost> Check(IHost host, ILoggerFactory? loggerFactory, IEnumerable<ServiceRequirement> requirements, string owner)
    {
        if (host is null)
            throw new ArgumentNullException(nameof(host));
        if (requirements is null)
            throw new ArgumentNullException(nameof(requirements));

        var isService = host.Services.GetRequiredService<IServiceProviderIsService>();
        var logger = loggerFactory?.CreateLogger(owner) ?? NullLogger.Instance;
        var seen = new HashSet<Type>();
        var missing = new List<IGenericMessage>();
        foreach (var requirement in requirements)
        {
            if (seen.Add(requirement.Service) && !isService.IsService(requirement.Service))
                missing.Add(requirement.Message(logger));
        }

        return missing.Count == 0
            ? GenericResult<IHost>.Success(host)
            : GenericResult<IHost>.Failure(missing);
    }
}

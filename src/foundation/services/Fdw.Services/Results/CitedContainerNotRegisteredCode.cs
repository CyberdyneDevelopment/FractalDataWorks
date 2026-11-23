using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Results;

/// <summary>
/// A record cites a member of a container, and no domain provider is registered for that container (FDW-6221).
/// </summary>
/// <remarks>
/// A domain's registration adds its provider to the set the citation check searches; a container nothing registered is a host that wires a citing
/// domain without the cited one, or an option that names a container no provider names. The failure names both so neither is guessed. HTTP 500.
/// </remarks>
[TypeOption(typeof(ServicesResultCodes), "CitedContainerNotRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CitedContainerNotRegisteredCode : ServicesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CitedContainerNotRegisteredCode"/> class.</summary>
    public CitedContainerNotRegisteredCode()
        : base(30001, "CitedContainerNotRegistered",
            ResultSeverities.ByName("Error"),
            "No domain provider is registered for the container '{container}' a citation names; the registered containers are: {registered}",
            isRetryable: false)
    {
    }
}

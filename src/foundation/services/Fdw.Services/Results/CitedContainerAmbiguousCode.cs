using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Results;

/// <summary>
/// More than one domain provider names the container a citation cites (FDW-6221).
/// </summary>
/// <remarks>Asking whichever came first would check a domain the citing record did not mean, so the check is refused. HTTP 500.</remarks>
[TypeOption(typeof(ServicesResultCodes), "CitedContainerAmbiguous", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CitedContainerAmbiguousCode : ServicesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CitedContainerAmbiguousCode"/> class.</summary>
    public CitedContainerAmbiguousCode()
        : base(41001, "CitedContainerAmbiguous",
            ResultSeverities.ByName("Error"),
            "{count} domain providers are registered for the container '{container}' a citation names; exactly one may be",
            isRetryable: false)
    {
    }
}

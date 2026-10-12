using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// No <see cref="Abstractions.Discovery.ISchemaDiscoverer"/> registered for the
/// supplied connection's runtime type.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DiscovererNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DiscovererNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DiscovererNotFoundCode"/> class.</summary>
    public DiscovererNotFoundCode()
        : base(60002, "DiscovererNotFound", ResultSeverities.ByName("Error"),
            "No schema discoverer registered for connection type '{ConnectionType}'",
            isRetryable: false)
    {
    }
}

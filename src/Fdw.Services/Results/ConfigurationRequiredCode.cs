using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// A configuration was required and none was supplied.
/// </summary>
/// <remarks>
/// Distinct from <c>ConfigurationNotFound</c>, and a caller acts on them differently: this one says
/// the call arrived with nothing, so no lookup was attempted and retrying cannot help. The other
/// says a lookup ran and came back empty, which a caller may reasonably resolve by another route.
/// </remarks>
[TypeOption(typeof(ServicesResultCodes), "ConfigurationRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationRequiredCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationRequiredCode"/> class.
    /// </summary>
    public ConfigurationRequiredCode()
        : base(21002, "ConfigurationRequired",
            ResultSeverities.ByName("Error"),
            "A configuration is required to create '{ServiceType}' and none was supplied",
            isRetryable: false)
    {
    }
}

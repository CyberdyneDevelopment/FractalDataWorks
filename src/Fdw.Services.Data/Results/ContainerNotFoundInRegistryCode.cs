using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Container was not found in registry.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerNotFoundInRegistry", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNotFoundInRegistryCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNotFoundInRegistryCode"/> class.
    /// </summary>
    public ContainerNotFoundInRegistryCode()
        : base(31003, "ContainerNotFoundInRegistry", ResultSeverities.ByName("Error"),
            "Container '{ContainerName}' not found in registry",
            isRetryable: false)
    {
    }
}
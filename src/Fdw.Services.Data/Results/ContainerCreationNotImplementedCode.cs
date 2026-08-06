using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Container creation is not implemented for the specified store type.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerCreationNotImplemented", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerCreationNotImplementedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerCreationNotImplementedCode"/> class.
    /// </summary>
    public ContainerCreationNotImplementedCode()
        : base(90005, "ContainerCreationNotImplemented", ResultSeverities.ByName("Error"),
            "Container creation for store type '{StoreType}' not yet implemented. Register a container factory for this store type.",
            isRetryable: false)
    {
    }
}
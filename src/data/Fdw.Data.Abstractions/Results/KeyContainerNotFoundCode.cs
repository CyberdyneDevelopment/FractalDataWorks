using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// The container a key operation targets was not found.
/// </summary>
[TypeOption(typeof(ContainerKeyResultCodes), "KeyContainerNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class KeyContainerNotFoundCode : ContainerKeyResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyContainerNotFoundCode"/> class.
    /// </summary>
    public KeyContainerNotFoundCode()
        : base(31000, "KeyContainerNotFound", ResultSeverities.ByName("Error"),
            "Container '{ContainerName}' not found",
            isRetryable: false)
    {
    }
}

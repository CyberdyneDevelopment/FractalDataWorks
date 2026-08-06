using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Failed to build container from configuration.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerBuildFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerBuildFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerBuildFailedCode"/> class.
    /// </summary>
    public ContainerBuildFailedCode()
        : base(91000, "ContainerBuildFailed", ResultSeverities.ByName("Error"),
            "Failed to build container '{ContainerName}': {Error}",
            isRetryable: false)
    {
    }
}
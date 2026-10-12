using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Container creation failed.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerCreationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerCreationFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerCreationFailedCode"/> class.
    /// </summary>
    public ContainerCreationFailedCode()
        : base(91001, "ContainerCreationFailed", ResultSeverities.ByName("Error"),
            "Failed to create container: {Error}",
            isRetryable: true)
    {
    }
}
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Container path was null or empty.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerPathRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerPathRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerPathRequiredCode"/> class.
    /// </summary>
    public ContainerPathRequiredCode()
        : base(21000, "ContainerPathRequired", ResultSeverities.ByName("Error"),
            "Container path cannot be null or empty",
            isRetryable: false)
    {
    }
}
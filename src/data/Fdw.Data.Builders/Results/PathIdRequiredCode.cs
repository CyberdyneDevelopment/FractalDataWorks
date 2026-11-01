using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Path ID is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "PathIdRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PathIdRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathIdRequiredCode"/> class.
    /// </summary>
    public PathIdRequiredCode()
        : base(21013, "PathIdRequired",
            ResultSeverities.ByName("Error"),
            "Path ID is required",
            isRetryable: false)
    {
    }
}
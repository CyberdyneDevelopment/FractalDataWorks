using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Path type is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "PathTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PathTypeRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathTypeRequiredCode"/> class.
    /// </summary>
    public PathTypeRequiredCode()
        : base(21015, "PathTypeRequired",
            ResultSeverities.ByName("Error"),
            "Path type is required",
            isRetryable: false)
    {
    }
}
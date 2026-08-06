using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Path name is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "PathNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PathNameRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathNameRequiredCode"/> class.
    /// </summary>
    public PathNameRequiredCode()
        : base(21014, "PathNameRequired",
            ResultSeverities.ByName("Error"),
            "Path name is required",
            isRetryable: false)
    {
    }
}
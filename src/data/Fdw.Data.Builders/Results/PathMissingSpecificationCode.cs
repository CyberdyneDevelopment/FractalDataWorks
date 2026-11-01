using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Path must have either FullPath or Segments specified.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "PathMissingSpecification", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PathMissingSpecificationCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathMissingSpecificationCode"/> class.
    /// </summary>
    public PathMissingSpecificationCode()
        : base(21016, "PathMissingSpecification",
            ResultSeverities.ByName("Error"),
            "Path must have either FullPath or Segments specified",
            isRetryable: false)
    {
    }
}
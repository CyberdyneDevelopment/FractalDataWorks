using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// ProjectName must be provided.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "ProjectNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNameRequiredCode"/> class.
    /// </summary>
    public ProjectNameRequiredCode()
        : base(21026, "ProjectNameRequired",
            ResultSeverities.ByName("Error"),
            "ProjectName must be provided",
            isRetryable: false)
    {
    }
}

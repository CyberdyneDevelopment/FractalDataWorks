using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Project not found.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "ProjectNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNotFoundCode"/> class.
    /// </summary>
    public ProjectNotFoundCode()
        : base(31015, "ProjectNotFound",
            ResultSeverities.ByName("Error"),
            "Project not found: {ProjectName}",
            isRetryable: false)
    {
    }
}

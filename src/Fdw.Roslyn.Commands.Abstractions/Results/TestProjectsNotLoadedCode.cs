using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// MoveNamespace was invoked against a workspace loaded without its test projects.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TestProjectsNotLoaded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TestProjectsNotLoadedCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestProjectsNotLoadedCode"/> class.
    /// </summary>
    public TestProjectsNotLoadedCode()
        : base(31021, "TestProjectsNotLoaded",
            ResultSeverities.ByName("Error"),
            "MoveNamespace rewrites references solution-wide and cannot run against a workspace loaded without test projects ({ExcludedCount} excluded); reload including tests",
            isRetryable: false)
    {
    }
}

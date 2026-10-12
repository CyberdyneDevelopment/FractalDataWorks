using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Test project not found.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TestProjectNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TestProjectNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestProjectNotFoundCode"/> class.
    /// </summary>
    public TestProjectNotFoundCode()
        : base(31018, "TestProjectNotFound",
            ResultSeverities.ByName("Error"),
            "Test project not found: {TestProjectName}",
            isRetryable: false)
    {
    }
}

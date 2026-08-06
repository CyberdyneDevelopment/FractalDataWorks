using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No public methods found to generate tests for.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoPublicMethodsFoundToGenerateTests", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoPublicMethodsFoundToGenerateTestsCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoPublicMethodsFoundToGenerateTestsCode"/> class.
    /// </summary>
    public NoPublicMethodsFoundToGenerateTestsCode()
        : base(31007, "NoPublicMethodsFoundToGenerateTests",
            ResultSeverities.ByName("Error"),
            "No public methods found to generate tests for",
            isRetryable: false)
    {
    }
}

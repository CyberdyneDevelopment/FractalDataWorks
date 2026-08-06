using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Variable must have an initializer to be inlined.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "VariableMustHaveInitializerToBeInlined", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VariableMustHaveInitializerToBeInlinedCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableMustHaveInitializerToBeInlinedCode"/> class.
    /// </summary>
    public VariableMustHaveInitializerToBeInlinedCode()
        : base(21022, "VariableMustHaveInitializerToBeInlined",
            ResultSeverities.ByName("Error"),
            "Variable must have an initializer to be inlined",
            isRetryable: false)
    {
    }
}

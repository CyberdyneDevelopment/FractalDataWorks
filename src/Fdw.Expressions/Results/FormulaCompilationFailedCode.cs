using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Expressions.Results;

/// <summary>
/// Formula compilation failed.
/// </summary>
[TypeOption(typeof(ExpressionResultCodes), "FormulaCompilationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FormulaCompilationFailedCode : ExpressionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormulaCompilationFailedCode"/> class.
    /// </summary>
    public FormulaCompilationFailedCode()
        : base(90003, "FormulaCompilationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to compile formula: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
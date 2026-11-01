using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No statements found in selected range.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoStatementsFoundInSelectedRange", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoStatementsFoundInSelectedRangeCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoStatementsFoundInSelectedRangeCode"/> class.
    /// </summary>
    public NoStatementsFoundInSelectedRangeCode()
        : base(31009, "NoStatementsFoundInSelectedRange",
            ResultSeverities.ByName("Error"),
            "No statements found in selected range",
            isRetryable: false)
    {
    }
}

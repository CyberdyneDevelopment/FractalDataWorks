using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Symbol at position is not a local variable.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SymbolNotLocalVariable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SymbolNotLocalVariableCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolNotLocalVariableCode"/> class.
    /// </summary>
    public SymbolNotLocalVariableCode()
        : base(21019, "SymbolNotLocalVariable",
            ResultSeverities.ByName("Error"),
            "Symbol at position is not a local variable",
            isRetryable: false)
    {
    }
}

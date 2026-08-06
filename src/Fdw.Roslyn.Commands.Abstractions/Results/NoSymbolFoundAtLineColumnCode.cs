using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No symbol found at line and column.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoSymbolFoundAtLineColumn", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoSymbolFoundAtLineColumnCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSymbolFoundAtLineColumnCode"/> class.
    /// </summary>
    public NoSymbolFoundAtLineColumnCode()
        : base(31010, "NoSymbolFoundAtLineColumn",
            ResultSeverities.ByName("Error"),
            "No symbol found at line {Line}, column {Column}",
            isRetryable: false)
    {
    }
}

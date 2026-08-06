using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Change ledger is not available.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "LedgerNotAvailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LedgerNotAvailableCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerNotAvailableCode"/> class.
    /// </summary>
    public LedgerNotAvailableCode()
        : base(70000, "LedgerNotAvailable",
            ResultSeverities.ByName("Error"),
            "Change ledger is not available",
            isRetryable: false)
    {
    }
}

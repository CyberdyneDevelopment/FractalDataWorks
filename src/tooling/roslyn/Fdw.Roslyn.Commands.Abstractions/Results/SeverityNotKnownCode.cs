using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// A diagnostics request names a severity that is not one of the levels (FDW-5194).
/// </summary>
/// <remarks>The request is refused rather than read with a value it did not state.</remarks>
[TypeOption(typeof(RoslynResultCodes), "SeverityNotKnown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SeverityNotKnownCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeverityNotKnownCode"/> class.
    /// </summary>
    public SeverityNotKnownCode()
        : base(21028, "SeverityNotKnown",
            ResultSeverities.ByName("Error"),
            "'{Severity}' is not a severity; use one of: {Accepted}",
            isRetryable: false)
    {
    }
}

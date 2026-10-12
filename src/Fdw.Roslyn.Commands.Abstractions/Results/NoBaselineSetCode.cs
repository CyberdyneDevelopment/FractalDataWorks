using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No baseline set - cannot revert.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoBaselineSet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoBaselineSetCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoBaselineSetCode"/> class.
    /// </summary>
    public NoBaselineSetCode()
        : base(40000, "NoBaselineSet",
            ResultSeverities.ByName("Error"),
            "No baseline set - cannot revert",
            isRetryable: false)
    {
    }
}

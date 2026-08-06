using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to get compilation.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToGetCompilation", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToGetCompilationCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToGetCompilationCode"/> class.
    /// </summary>
    public FailedToGetCompilationCode()
        : base(91004, "FailedToGetCompilation",
            ResultSeverities.ByName("Error"),
            "Failed to get compilation",
            isRetryable: false)
    {
    }
}

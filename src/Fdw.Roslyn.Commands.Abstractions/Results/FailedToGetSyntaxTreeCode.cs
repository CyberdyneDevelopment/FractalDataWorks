using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to get syntax tree.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToGetSyntaxTree", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToGetSyntaxTreeCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToGetSyntaxTreeCode"/> class.
    /// </summary>
    public FailedToGetSyntaxTreeCode()
        : base(91007, "FailedToGetSyntaxTree",
            ResultSeverities.ByName("Error"),
            "Failed to get syntax tree",
            isRetryable: false)
    {
    }
}

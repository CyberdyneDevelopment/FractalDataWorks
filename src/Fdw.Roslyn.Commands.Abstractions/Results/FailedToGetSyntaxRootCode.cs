using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to get syntax root.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToGetSyntaxRoot", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToGetSyntaxRootCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToGetSyntaxRootCode"/> class.
    /// </summary>
    public FailedToGetSyntaxRootCode()
        : base(91006, "FailedToGetSyntaxRoot",
            ResultSeverities.ByName("Error"),
            "Failed to get syntax root",
            isRetryable: false)
    {
    }
}

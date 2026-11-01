using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No unresolved-reference diagnostics were found in scope, so there is nothing to repair.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoReferenceErrorsFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoReferenceErrorsFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoReferenceErrorsFoundCode"/> class.
    /// </summary>
    public NoReferenceErrorsFoundCode()
        : base(31025, "NoReferenceErrorsFound",
            ResultSeverities.ByName("Error"),
            "No unresolved-reference diagnostics found in scope: {Scope}",
            isRetryable: false)
    {
    }
}

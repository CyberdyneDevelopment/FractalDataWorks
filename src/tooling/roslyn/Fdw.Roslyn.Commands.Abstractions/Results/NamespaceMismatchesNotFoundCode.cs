using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No type in scope has a namespace that disagrees with its path or project.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NamespaceMismatchesNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NamespaceMismatchesNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceMismatchesNotFoundCode"/> class.
    /// </summary>
    public NamespaceMismatchesNotFoundCode()
        : base(31019, "NamespaceMismatchesNotFound",
            ResultSeverities.ByName("Error"),
            "No namespace mismatches found in scope: {Scope}",
            isRetryable: false)
    {
    }
}

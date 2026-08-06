using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Namespace is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NamespaceRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NamespaceRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceRequiredCode"/> class.
    /// </summary>
    public NamespaceRequiredCode()
        : base(21007, "NamespaceRequired",
            ResultSeverities.ByName("Error"),
            "Namespace is required",
            isRetryable: false)
    {
    }
}

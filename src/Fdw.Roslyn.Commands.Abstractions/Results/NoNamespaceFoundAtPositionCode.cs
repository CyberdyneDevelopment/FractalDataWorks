using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No namespace found at position.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoNamespaceFoundAtPosition", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoNamespaceFoundAtPositionCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoNamespaceFoundAtPositionCode"/> class.
    /// </summary>
    public NoNamespaceFoundAtPositionCode()
        : base(31005, "NoNamespaceFoundAtPosition",
            ResultSeverities.ByName("Error"),
            "No namespace found at position (global namespace not supported)",
            isRetryable: false)
    {
    }
}

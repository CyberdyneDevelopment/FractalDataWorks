using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No type declaration found at position.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoTypeDeclarationFoundAtPosition", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoTypeDeclarationFoundAtPositionCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoTypeDeclarationFoundAtPositionCode"/> class.
    /// </summary>
    public NoTypeDeclarationFoundAtPositionCode()
        : base(31013, "NoTypeDeclarationFoundAtPosition",
            ResultSeverities.ByName("Error"),
            "No type declaration found at position",
            isRetryable: false)
    {
    }
}

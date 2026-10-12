using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No declaration found for symbol.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoDeclarationFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoDeclarationFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoDeclarationFoundCode"/> class.
    /// </summary>
    public NoDeclarationFoundCode()
        : base(31003, "NoDeclarationFound",
            ResultSeverities.ByName("Error"),
            "No declaration found for '{SymbolName}'",
            isRetryable: false)
    {
    }
}

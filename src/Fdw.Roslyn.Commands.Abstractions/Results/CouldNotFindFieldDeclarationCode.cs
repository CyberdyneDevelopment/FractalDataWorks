using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Could not find field declaration.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "CouldNotFindFieldDeclaration", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CouldNotFindFieldDeclarationCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CouldNotFindFieldDeclarationCode"/> class.
    /// </summary>
    public CouldNotFindFieldDeclarationCode()
        : base(31000, "CouldNotFindFieldDeclaration",
            ResultSeverities.ByName("Error"),
            "Could not find field declaration",
            isRetryable: false)
    {
    }
}

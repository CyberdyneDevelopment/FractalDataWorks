using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No project moves were specified in the command.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoMovesSpecified", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoMovesSpecifiedCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoMovesSpecifiedCode"/> class.
    /// </summary>
    public NoMovesSpecifiedCode()
        : base(21008, "NoMovesSpecified",
            ResultSeverities.ByName("Error"),
            "No project moves were specified",
            isRetryable: false)
    {
    }
}

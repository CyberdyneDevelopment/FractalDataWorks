using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No symbol found at offset position.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoSymbolFoundAtOffset", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoSymbolFoundAtOffsetCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSymbolFoundAtOffsetCode"/> class.
    /// </summary>
    public NoSymbolFoundAtOffsetCode()
        : base(31011, "NoSymbolFoundAtOffset",
            ResultSeverities.ByName("Error"),
            "No symbol found at position {Position}",
            isRetryable: false)
    {
    }
}

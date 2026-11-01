using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No source definition found for symbol.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoSourceDefinitionFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoSourceDefinitionFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSourceDefinitionFoundCode"/> class.
    /// </summary>
    public NoSourceDefinitionFoundCode()
        : base(31008, "NoSourceDefinitionFound",
            ResultSeverities.ByName("Error"),
            "No source definition found for '{SymbolName}'",
            isRetryable: false)
    {
    }
}

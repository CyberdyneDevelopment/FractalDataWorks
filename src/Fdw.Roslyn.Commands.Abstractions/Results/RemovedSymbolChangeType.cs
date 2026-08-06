using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>The symbol was removed.</summary>
[TypeOption(typeof(SymbolChangeTypes), "Removed")]
[ExcludeFromCodeCoverage]
public sealed class RemovedSymbolChangeType : SymbolChangeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="RemovedSymbolChangeType"/>.</summary>
    public RemovedSymbolChangeType() : base(4, "Removed") { }
}

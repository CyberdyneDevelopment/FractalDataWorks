using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>The symbol was renamed.</summary>
[TypeOption(typeof(SymbolChangeTypes), "Renamed")]
[ExcludeFromCodeCoverage]
public sealed class RenamedSymbolChangeType : SymbolChangeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="RenamedSymbolChangeType"/>.</summary>
    public RenamedSymbolChangeType() : base(1, "Renamed") { }
}

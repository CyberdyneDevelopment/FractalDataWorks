using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>The symbol was moved to a different file.</summary>
[TypeOption(typeof(SymbolChangeTypes), "Moved")]
[ExcludeFromCodeCoverage]
public sealed class MovedSymbolChangeType : SymbolChangeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="MovedSymbolChangeType"/>.</summary>
    public MovedSymbolChangeType() : base(2, "Moved") { }
}

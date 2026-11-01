using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Base class for symbol change types.
/// </summary>
public abstract class SymbolChangeTypeBase : TypeOptionBase<SymbolChangeTypeBase>, ISymbolChangeType
{
    /// <summary>
    /// Initializes a new instance of <see cref="SymbolChangeTypeBase"/>.
    /// </summary>
    protected SymbolChangeTypeBase(string name) : base(name) { }
}

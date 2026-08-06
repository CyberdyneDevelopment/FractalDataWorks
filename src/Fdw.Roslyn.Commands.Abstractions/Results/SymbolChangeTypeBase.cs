using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Base class for symbol change types.
/// </summary>
public abstract class SymbolChangeTypeBase : TypeOptionBase<int, SymbolChangeTypeBase>, ISymbolChangeType
{
    /// <summary>
    /// Initializes a new instance of <see cref="SymbolChangeTypeBase"/>.
    /// </summary>
    protected SymbolChangeTypeBase(int id, string name) : base(id, name) { }
}

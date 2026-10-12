using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// TypeCollection for symbol change types.
/// </summary>
[TypeCollection(typeof(SymbolChangeTypeBase), typeof(ISymbolChangeType), typeof(SymbolChangeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class SymbolChangeTypes : TypeCollectionBase<SymbolChangeTypeBase, ISymbolChangeType> { }

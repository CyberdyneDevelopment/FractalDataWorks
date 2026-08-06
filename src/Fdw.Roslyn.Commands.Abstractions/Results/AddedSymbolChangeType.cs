using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>The symbol was added (e.g. extracted into a new interface, method, or property).</summary>
[TypeOption(typeof(SymbolChangeTypes), "Added")]
[ExcludeFromCodeCoverage]
public sealed class AddedSymbolChangeType : SymbolChangeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="AddedSymbolChangeType"/>.</summary>
    public AddedSymbolChangeType() : base(3, "Added") { }
}

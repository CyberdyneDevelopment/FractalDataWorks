using Fdw.Collections;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Base class for <see cref="ISymbolKindScore"/> TypeOptions.</summary>
public abstract class SymbolKindScoreBase : TypeOptionBase<ISymbolKindScore>, ISymbolKindScore
{
    /// <summary>
    /// Required protected parameterless constructor for the TypeCollection Empty sentinel.
    /// Returns <see cref="Weight"/> = 0 so unmatched symbol kinds contribute nothing to the score.
    /// </summary>
    protected SymbolKindScoreBase() : base(0, "NotFound")
    {
        Weight = 0;
    }

    /// <summary>Initializes a kind score.</summary>
    /// <param name="name">Roslyn <c>SymbolKind</c> name (matches <c>SymbolKind.ToString()</c>).</param>
    /// <param name="weight">Scoring weight for this kind.</param>
    protected SymbolKindScoreBase(string name, int weight) : base(name)
    {
        Weight = weight;
    }

    /// <inheritdoc />
    public int Weight { get; }
}

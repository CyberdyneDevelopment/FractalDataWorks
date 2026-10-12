using Fdw.Collections;

namespace Fdw.Services.Workspaces.Lineage;

/// <summary>
/// A way the lineage view walks from its focus. Each direction says which sides it includes, so the walk never switches
/// on the direction itself.
/// </summary>
public interface ILineageDirection : ITypeOption<int, LineageDirectionBase>
{
    /// <summary>Gets the word a request names the direction by, in the case the API documents.</summary>
    string Wire { get; }

    /// <summary>Gets a value indicating whether the walk goes toward what feeds the focus.</summary>
    bool IncludesUpstream { get; }

    /// <summary>Gets a value indicating whether the walk goes toward what the focus feeds.</summary>
    bool IncludesDownstream { get; }
}

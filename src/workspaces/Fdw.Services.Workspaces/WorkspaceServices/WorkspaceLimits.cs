namespace Fdw.Services.Workspaces;

/// <summary>
/// The limits the workspace endpoints are held to (FDW-7104): how large a lineage neighbourhood, a path and a search page may be, and how long a note.
/// </summary>
/// <remarks>
/// Configuration, never code: the defaults are the workspace service type option's declared defaults (<c>LineageMaxNodes</c>, <c>LineageMaxDepth</c>,
/// <c>LineageMaxPathDepth</c>, <c>LineageSearchDefaultTake</c>, <c>LineageSearchMaxTake</c>, <c>NoteMaxBodyLength</c>), a host changes one at registration with
/// <c>ServiceTypeDefaults.Override</c>, and a value stated under <c>Workspaces:Default</c> wins. The registration builds one of these from what applies.
/// </remarks>
public sealed class WorkspaceLimits
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceLimits"/> class.</summary>
    /// <param name="lineageMaxNodes">The most nodes one lineage neighbourhood answers with; past it the answer says it was cut short.</param>
    /// <param name="lineageMaxDepth">The deepest a lineage neighbourhood may walk.</param>
    /// <param name="lineageMaxPathDepth">The deepest a lineage path search may go, and the depth it takes when the caller states none.</param>
    /// <param name="lineageSearchDefaultTake">How many lineage search hits come back when the caller names no count.</param>
    /// <param name="lineageSearchMaxTake">The most lineage search hits one call may ask for.</param>
    /// <param name="noteMaxBodyLength">The longest workspace note body accepted, in characters.</param>
    public WorkspaceLimits(int lineageMaxNodes, int lineageMaxDepth, int lineageMaxPathDepth, int lineageSearchDefaultTake, int lineageSearchMaxTake, int noteMaxBodyLength)
    {
        LineageMaxNodes = lineageMaxNodes;
        LineageMaxDepth = lineageMaxDepth;
        LineageMaxPathDepth = lineageMaxPathDepth;
        LineageSearchDefaultTake = lineageSearchDefaultTake;
        LineageSearchMaxTake = lineageSearchMaxTake;
        NoteMaxBodyLength = noteMaxBodyLength;
    }

    /// <summary>Gets the most nodes one lineage neighbourhood answers with.</summary>
    public int LineageMaxNodes { get; }

    /// <summary>Gets the deepest a lineage neighbourhood may walk.</summary>
    public int LineageMaxDepth { get; }

    /// <summary>Gets the deepest a lineage path search may go, and the depth it takes when the caller states none.</summary>
    public int LineageMaxPathDepth { get; }

    /// <summary>Gets how many lineage search hits come back when the caller names no count.</summary>
    public int LineageSearchDefaultTake { get; }

    /// <summary>Gets the most lineage search hits one call may ask for.</summary>
    public int LineageSearchMaxTake { get; }

    /// <summary>Gets the longest workspace note body accepted, in characters.</summary>
    public int NoteMaxBodyLength { get; }
}

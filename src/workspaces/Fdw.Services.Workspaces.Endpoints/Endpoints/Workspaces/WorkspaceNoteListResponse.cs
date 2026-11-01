using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A workspace's notes, newest first.</summary>
/// <remarks>
/// The ordering is part of the contract rather than left to the client: sorting on a timestamp
/// whose precision and clock source the client does not know is a guess dressed as a sort.
/// </remarks>
public class WorkspaceNoteListResponse
{
    /// <summary>Gets or sets the notes, newest first.</summary>
    public IList<WorkspaceNoteResponse> Items { get; set; } = [];
}

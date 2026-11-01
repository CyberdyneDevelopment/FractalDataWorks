using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a caller supplies to write a note.</summary>
/// <remarks>
/// Deliberately three fields. The author comes from the token, the workspace from the route and the
/// timestamp from the server clock — if a client can send an author or a time, a client can lie
/// about both.
/// </remarks>
public class CreateWorkspaceNoteRequest
{
    /// <summary>Gets or sets the workspace the note is raised in, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the note text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets what kind of thing the note is about.</summary>
    public string SubjectKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the subject's identity within that kind.</summary>
    public Guid SubjectKey { get; set; }
}

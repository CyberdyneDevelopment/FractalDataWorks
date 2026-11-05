namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>The query of the alerts list (FDW-3863). Every member is optional.</summary>
public sealed class EscalationAlertListRequest
{
    /// <summary>Gets or sets one state to list: Firing, Acknowledged or Resolved. Left out lists every alert. Not together with <see cref="Open"/>.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets whether to list the open alerts (Firing and Acknowledged, true) or the closed ones (Resolved, false). Not together with <see cref="State"/>.</summary>
    public bool? Open { get; set; }

    /// <summary>Gets or sets the escalation policy's name to list the alerts of.</summary>
    public string? Policy { get; set; }

    /// <summary>Gets or sets the owner's kind to list the alerts of: Server, Pipeline, Schedule or Component.</summary>
    public string? SubjectKind { get; set; }

    /// <summary>Gets or sets the owner's name to list the alerts of.</summary>
    public string? SubjectName { get; set; }

    /// <summary>Gets or sets how many alerts to skip, newest first. Left out skips none.</summary>
    public int? Skip { get; set; }

    /// <summary>Gets or sets how many alerts to return. Left out takes the setting DefaultPaginationSize; when that is not set either, every alert is returned.</summary>
    public int? Take { get; set; }
}

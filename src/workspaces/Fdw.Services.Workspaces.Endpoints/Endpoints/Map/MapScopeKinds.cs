namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a map response says it covers.</summary>
public static class MapScopeKinds
{
    /// <summary>Everything the caller can see.</summary>
    public const string Everything = "everything";

    /// <summary>The pipelines of one organization and what they reach.</summary>
    public const string Organization = "organization";

    /// <summary>The resources one workspace holds, and its declared relationships.</summary>
    public const string Workspace = "workspace";

    /// <summary>The children of one node, as an explode answers.</summary>
    public const string Children = "children";
}

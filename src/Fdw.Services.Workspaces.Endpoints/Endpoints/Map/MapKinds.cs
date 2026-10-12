namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The kinds of node the map draws, as the strings a node carries.</summary>
/// <remarks>
/// The first three are the names <c>workspace.WorkspaceResource.ResourceType</c> already uses, so a node means the same
/// thing whether it was reached through a workspace or not. A resource of any other kind a workspace holds keeps its own
/// kind name on its node: the set of kinds is open.
/// </remarks>
public static class MapKinds
{
    /// <summary>A data set.</summary>
    public const string DataSet = "DataSet";

    /// <summary>A data store.</summary>
    public const string DataStore = "DataStore";

    /// <summary>A pipeline.</summary>
    public const string Pipeline = "Pipeline";

    /// <summary>A container inside a data store, which only an explode brings onto the map.</summary>
    public const string Container = "Container";

    /// <summary>A field of a data set, which only an explode with <c>kinds=DataSetField</c> brings onto the map.</summary>
    public const string DataSetField = "DataSetField";

    /// <summary>A connection, counted from its domain table by the summary; the map draws a connection as part of its data store, not as a node.</summary>
    public const string Connection = "Connection";

    /// <summary>A quality rule, counted from its domain table by the summary; the map does not draw one.</summary>
    public const string QualityRule = "QualityRule";

    /// <summary>A schedule, counted from its domain table by the summary; the map does not draw one.</summary>
    public const string Schedule = "Schedule";
}

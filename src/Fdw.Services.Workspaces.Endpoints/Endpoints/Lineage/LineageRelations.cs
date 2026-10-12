namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The relations a lineage edge carries.</summary>
public static class LineageRelations
{
    /// <summary>Data moves from one data set to another through a pipeline; the edge names the pipeline in its via.</summary>
    public const string Pipeline = "pipeline";

    /// <summary>A data set built from another: the edge runs from the origin to the derived data set.</summary>
    public const string DerivedFrom = "derived_from";

    /// <summary>A data set stored in a data store: the edge runs from the data store to the data set.</summary>
    public const string StoredIn = "stored_in";
}

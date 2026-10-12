namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The relationship types an edge on the map can carry.</summary>
public static class MapRelations
{
    /// <summary>A data set a pipeline reads: the edge runs from the data set to the pipeline.</summary>
    public const string ConsumedBy = "consumed_by";

    /// <summary>A data set a pipeline writes: the edge runs from the pipeline to the data set.</summary>
    public const string Produces = "produces";

    /// <summary>Where a data set is stored: the edge runs from the data set to a data store, or to a container once exploded.</summary>
    public const string StoredIn = "stored_in";

    /// <summary>A container inside a data store: the edge runs from the container to the data store.</summary>
    public const string PartOf = "part_of";

    /// <summary>A data set built from another: the edge runs from the derived data set to the one it comes from.</summary>
    public const string DerivedFrom = "derived_from";

    /// <summary>A join a workspace declares between two data sets.</summary>
    public const string RelatesTo = "relates_to";
}

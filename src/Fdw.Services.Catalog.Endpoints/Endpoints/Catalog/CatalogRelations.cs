using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>What one named thing of the catalog does with another (FDW-1683).</summary>
public static class CatalogRelations
{
    /// <summary>Reads the other: a pipeline's source, a calculation's input.</summary>
    public const string Reads = PipelineUseRelations.Reads;

    /// <summary>Writes the other: a pipeline's destination, a calculation's output data set.</summary>
    public const string Writes = PipelineUseRelations.Writes;

    /// <summary>Starts the other: a pipeline that runs another pipeline.</summary>
    public const string Calls = PipelineUseRelations.Calls;

    /// <summary>Runs something against the other that is neither a read nor a write of rows: a SQL step on a data store.</summary>
    public const string Uses = PipelineUseRelations.Uses;

    /// <summary>Explains the other: a glossary term linked to a data set or one of its fields.</summary>
    public const string Describes = "describes";

    /// <summary>Is built from the other: a data set derived from another data set.</summary>
    public const string DerivedFrom = "derivedFrom";

    /// <summary>Is stored in the other: a data set held in a data store.</summary>
    public const string StoredIn = "storedIn";
}

namespace Fdw.Services.Etl.Graphs;

/// <summary>What a pipeline does with a thing it uses (FDW-1683).</summary>
public static class PipelineUseRelations
{
    /// <summary>The pipeline reads it.</summary>
    public const string Reads = "reads";

    /// <summary>The pipeline writes it.</summary>
    public const string Writes = "writes";

    /// <summary>The pipeline starts it: another pipeline.</summary>
    public const string Calls = "calls";

    /// <summary>The pipeline runs something against it that is neither a read nor a write of rows: a SQL step on a data store.</summary>
    public const string Uses = "uses";
}

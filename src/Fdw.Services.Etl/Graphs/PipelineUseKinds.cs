namespace Fdw.Services.Etl.Graphs;

/// <summary>The kinds of thing a pipeline uses, named as the catalog names its kinds (FDW-1683).</summary>
public static class PipelineUseKinds
{
    /// <summary>A data set.</summary>
    public const string DataSet = "DataSet";

    /// <summary>A data store.</summary>
    public const string DataStore = "DataStore";

    /// <summary>A container in a data store.</summary>
    public const string Container = "Container";

    /// <summary>Another pipeline.</summary>
    public const string Pipeline = "Pipeline";

    /// <summary>A transformation a tool names.</summary>
    public const string Transformation = "Transformation";
}

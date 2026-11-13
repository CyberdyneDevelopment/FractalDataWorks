namespace Fdw.Data.DataSets;

/// <summary>The kinds of thing a Graph data set reads, named as the pipeline uses name theirs (FDW-5296).</summary>
public static class GraphDataSetReadKinds
{
    /// <summary>A data set.</summary>
    public const string DataSet = "DataSet";

    /// <summary>A data store.</summary>
    public const string DataStore = "DataStore";

    /// <summary>A container in a data store.</summary>
    public const string Container = "Container";
}

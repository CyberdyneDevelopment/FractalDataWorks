namespace Fdw.Services.Data;

/// <summary>Which side of a DataSet's lineage a closure read is filtered to.</summary>
public enum LineageClosureDirection
{
    /// <summary>Everything downstream of the given DataSet — it is the ancestor of every result.</summary>
    Downstream,

    /// <summary>Everything upstream of the given DataSet — it is the descendant of every result.</summary>
    Upstream,
}

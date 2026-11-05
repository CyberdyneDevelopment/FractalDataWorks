namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The two kinds of variation, told apart by who owns the variation.</summary>
public static class VariationKinds
{
    /// <summary>A data set a workspace owns as its own view of a main data set.</summary>
    public const string Scoped = "Scoped";

    /// <summary>A saved chart of a data set, owned by a user.</summary>
    public const string DataView = "DataView";
}

namespace Fdw.Schema;

/// <summary>The names of the aggregates a datum type can allow.</summary>
/// <remarks>
/// They are the option names of the ETL layer's <c>AggregateFunctions</c> collection, which this project cannot
/// reference, so a function named here and a function named there are the same by spelling.
/// </remarks>
public static class DatumAggregates
{
    /// <summary>Counts rows. Any datum type allows it.</summary>
    public const string Count = "Count";

    /// <summary>Adds numbers up. Only an aggregatable datum type allows it.</summary>
    public const string Sum = "Sum";

    /// <summary>Averages numbers. Only an aggregatable datum type allows it.</summary>
    public const string Avg = "Avg";

    /// <summary>Takes the smallest value. Only an orderable datum type allows it.</summary>
    public const string Min = "Min";

    /// <summary>Takes the largest value. Only an orderable datum type allows it.</summary>
    public const string Max = "Max";
}

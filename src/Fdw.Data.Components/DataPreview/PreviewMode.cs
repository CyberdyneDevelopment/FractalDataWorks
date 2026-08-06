namespace Fdw.Data.Components.DataPreview;

/// <summary>
/// String constants matching the <see cref="PreviewModes"/> TypeCollection option names.
/// Use these for string comparisons in consumer markup where TypeCollection lookup is unnecessary.
/// </summary>
public static class PreviewMode
{
    /// <summary>Preview data from a DataStore container.</summary>
    public const string DataStore = "DataStore";

    /// <summary>Preview data from a raw connection table or view.</summary>
    public const string Table = "Table";

    /// <summary>Preview data from a configured DataSet.</summary>
    public const string DataSet = "DataSet";
}

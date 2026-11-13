namespace Fdw.Data.Options;

/// <summary>
/// The names of the fields a stored option row has. FDW does not own the table that holds the rows; a consumer creates it with these
/// fields, and whatever reads the rows (the <c>ReadDataOptions</c> pipeline step) reads them by these names.
/// </summary>
public static class DataOptionRowFields
{
    /// <summary>The name of the data store that holds the column's container.</summary>
    public const string DataStore = "DataStore";

    /// <summary>The name of the data container the column belongs to.</summary>
    public const string Container = "Container";

    /// <summary>The name of the column the row is about.</summary>
    public const string Column = "Column";

    /// <summary>The value the option applies to.</summary>
    public const string Value = "Value";

    /// <summary>The option type, a <see cref="DataOptions"/> name.</summary>
    public const string OptionType = "OptionType";

    /// <summary>The option, a name in the option type's own collection.</summary>
    public const string Option = "Option";

    /// <summary>Whether the row is switched on; an inactive row changes nothing.</summary>
    public const string Active = "Active";
}

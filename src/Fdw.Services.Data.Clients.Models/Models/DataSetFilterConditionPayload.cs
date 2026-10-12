namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// A filter condition applied to a DataSet query or data preview.
/// </summary>
public sealed class DataSetFilterConditionPayload
{
    /// <summary>Gets or sets the field name to filter on.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter operator.
    /// Valid values: Equals, NotEquals, Contains, StartsWith, EndsWith,
    /// GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, IsNull, IsNotNull.
    /// </summary>
    public string Operator { get; set; } = "Equals";

    /// <summary>Gets or sets the comparison value. Null for IsNull/IsNotNull operators.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the data type of the field, used to select appropriate operators in the UI.</summary>
    public string DataType { get; set; } = "String";
}

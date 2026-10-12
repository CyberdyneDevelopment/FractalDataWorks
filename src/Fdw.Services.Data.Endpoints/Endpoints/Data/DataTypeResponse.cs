namespace Fdw.Services.Data.Endpoints;

/// <summary>One portable data type, as offered to the people typing fields (FDW-1103, FDW-1159).</summary>
public class DataTypeResponse
{
    /// <summary>
    /// Gets or sets the name a field states: <c>FrameworkDataType</c> on a container field, <c>dataType</c> on a
    /// data set field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified name of the .NET type the data type's values have, such as
    /// <c>System.Int32</c>. For information: a field never states it.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}

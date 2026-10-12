namespace Fdw.Services.Data.Endpoints;

/// <summary>One field of a data set and its stand-in, if it has one.</summary>
public class DataSetFieldStandInEntry
{
    /// <summary>Gets or sets the field's own logical id.</summary>
    public System.Guid FieldId { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's active stand-in, or null when it has none (not an error).</summary>
    public DataSetFieldStandInResponse? StandIn { get; set; }
}

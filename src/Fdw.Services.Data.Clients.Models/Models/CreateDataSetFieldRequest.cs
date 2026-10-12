using Fdw.Data.Abstractions;
using Fdw.Schema;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to create a DataSet field.
/// </summary>
public sealed class CreateDataSetFieldRequest
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the field's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>
    /// Gets or sets the <c>DatumTypeId</c> of what the field's data is (see <c>GET /datum-types</c>). Left out,
    /// it is <c>NotYetTyped</c>. An id that is not a datum type is refused (FDW-988).
    /// </summary>
    public int DatumTypeId { get; set; } = DatumTypes.NotYetTyped.Id;
    /// <summary>
    /// Gets or sets the field's data type: the name of a <c>DataTypes</c> option, such as <c>Int32</c>,
    /// <c>String</c> or <c>DateTimeOffset</c> (see <c>GET /data-types</c>). Left out, the field is not typed yet.
    /// A name that is not an option, a CLR type name such as <c>System.String</c> included, is refused (FDW-1159).
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the field is a key.</summary>
    public bool IsKey { get; set; }
    /// <summary>Gets or sets whether the field is required.</summary>
    public bool IsRequired { get; set; }
    /// <summary>Gets or sets whether the field is indexed.</summary>
    public bool IsIndexed { get; set; }
    /// <summary>
    /// Gets or sets whether API callers may filter on the field. Required: a request that does not say is refused, because
    /// there is no default (FDW-1102).
    /// </summary>
    public bool? IsFilterable { get; set; }
    /// <summary>Gets or sets the maximum length.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Gets or sets the default value.</summary>
    public string? DefaultValue { get; set; }
    /// <summary>Gets or sets whether the field is calculated.</summary>
    public bool IsCalculated { get; set; }
    /// <summary>Gets or sets the name of the configured calculation that computes this field's value when <see cref="IsCalculated"/> is true.</summary>
    public string? CalculationName { get; set; }
    /// <summary>Gets or sets whether this field participates as a join key for cross-source joins.</summary>
    public bool IsJoinKey { get; set; }
    /// <summary>Gets or sets the property role.</summary>
    public string? Role { get; set; }
    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }
}

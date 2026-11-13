using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Schema;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A field out of a Graph data set as a request states it (FDW-5287): what a reader sees, and the component output that feeds it.</summary>
/// <remarks>
/// The members are those of a field of any data set (<c>CreateDataSetFieldRequest</c>) and are checked by the same rules; <see cref="FedByComponent"/> and
/// <see cref="FedByField"/> are the Graph kind's own, and are both stated or both left out (an abstract data set states neither).
/// </remarks>
public sealed class GraphFieldOutRequest
{
    /// <summary>Gets or sets the name of the field.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the field.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets how sensitive the field is: the <c>ClassificationId</c> of a <c>DataClassifications</c> level.</summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets what the field's data is: the <c>DatumTypeId</c> of a <c>DatumTypes</c> option.</summary>
    public int DatumTypeId { get; set; } = DatumTypes.NotYetTyped.Id;

    /// <summary>Gets or sets the name of the field's <c>DataTypes</c> option; empty until someone types it.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field may be null; a new field must state it.</summary>
    public bool? IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is part of the key.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets whether the field must be given.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the field is indexed.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>Gets or sets whether callers may filter on the field; a new field must state it.</summary>
    public bool? IsFilterable { get; set; }

    /// <summary>Gets or sets the longest value the field holds.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the name of the calculation that makes the field.</summary>
    public string? CalculationName { get; set; }

    /// <summary>Gets or sets whether the field joins its data set to another.</summary>
    public bool IsJoinKey { get; set; }

    /// <summary>Gets or sets the property role (Surrogate, NaturalKey, Lookup, Attribute, Measure).</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets the position of the field among the fields out.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the name of the component whose output feeds the field; left out while the data set is abstract.</summary>
    public string? FedByComponent { get; set; }

    /// <summary>Gets or sets the name of the output field of <see cref="FedByComponent"/> that feeds the field; left out while the data set is abstract.</summary>
    public string? FedByField { get; set; }

    /// <summary>Gets or sets the discussion on the field; left out, the stored notes are kept, and an empty list clears them.</summary>
    public IList<GraphFieldNoteRequest>? Notes { get; set; }
}

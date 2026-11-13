using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A field out of a Graph data set as the get-graph route answers it (FDW-5295): the members of <see cref="GraphFieldOutRequest"/>, its discussion, and its worked-out state.</summary>
/// <remarks>
/// The member names are those of the request, so a field read here can be sent back to create or update. <see cref="State"/> is worked out when the
/// field is read (<c>GraphDataSetFieldStates</c>), never stored, and is not read back by a save.
/// </remarks>
public sealed class GraphFieldOutPayload
{
    /// <summary>Gets or sets the name of the field.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the field.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets how sensitive the field is: the <c>ClassificationId</c> of a <c>DataClassifications</c> level.</summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets what the field's data is: the <c>DatumTypeId</c> of a <c>DatumTypes</c> option.</summary>
    public int DatumTypeId { get; set; }

    /// <summary>Gets or sets the name of the field's <c>DataTypes</c> option; empty until someone types it.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field may be null.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is part of the key.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets whether the field must be given.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the field is indexed.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>Gets or sets whether callers may filter on the field.</summary>
    public bool IsFilterable { get; set; }

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

    /// <summary>Gets or sets the name of the component whose output feeds the field; null while abstract, and null when the caller may not read the graph.</summary>
    public string? FedByComponent { get; set; }

    /// <summary>Gets or sets the name of the output field of <see cref="FedByComponent"/> that feeds the field; null while abstract, and null when the caller may not read the graph.</summary>
    public string? FedByField { get; set; }

    /// <summary>Gets or sets the discussion on the field, in the order it was written: the shape a request states it in.</summary>
    public IList<GraphFieldNoteRequest> Notes { get; set; } = [];

    /// <summary>Gets or sets the state of the field: the name of a <c>GraphFieldStates</c> option (QuestionOpen, Intended or Defined).</summary>
    public string State { get; set; } = string.Empty;
}

using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data;
using Fdw.Schema;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A field out of a Graph data set (FDW-5286): what a reader of the data set sees.</summary>
/// <remarks>
/// Maps to <c>data.GraphDataSetField</c>, owned by one <c>data.GraphDataSetConfiguration</c> row through
/// <c>GraphDataSetConfigurationRowId</c>. It has its own class and table because <see cref="DataSetFieldConfiguration"/> maps to
/// <c>data.DataSetField</c>, whose owner check admits only the Simple, Compound and Federated kinds, and an existing table does not
/// change for a new kind. <see cref="FedByComponent"/> and <see cref="FedByField"/> name the component output that feeds the field;
/// both are empty while the data set is abstract.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetFieldConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the stable logical identifier for this field record, minted by the app before INSERT.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the field.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the field.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets how sensitive this data is: the <c>ClassificationId</c> of a <c>DataClassifications</c> level.</summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets what this field's data is: the <c>DatumTypeId</c> of a <c>DatumTypes</c> option.</summary>
    public int DatumTypeId { get; set; } = DatumTypes.NotYetTyped.Id;

    /// <summary>Gets or sets the name of the field's <c>DataTypes</c> option; empty until someone types the field.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the property role (Surrogate, NaturalKey, Lookup, Attribute, Measure).</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets a value indicating whether this field is part of the key.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets a value indicating whether the field may be null.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets a value indicating whether the field must be given.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets a value indicating whether the system provides the field.</summary>
    public bool IsSystemProvided { get; set; }

    /// <summary>Gets or sets a value indicating whether the field is indexed.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>Gets or sets a value indicating whether callers may filter on the field.</summary>
    public bool IsFilterable { get; set; }

    /// <summary>Gets or sets the longest value the field holds.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the position of the field among its data set's fields.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets a value indicating whether the field joins its data set to another.</summary>
    public bool IsJoinKey { get; set; }

    /// <summary>Gets or sets the name of the calculation that makes the field.</summary>
    public string? CalculationName { get; set; }

    /// <summary>Gets or sets the name of the component whose output feeds this field; null while the data set is abstract.</summary>
    public string? FedByComponent { get; set; }

    /// <summary>Gets or sets the name of the output field of <see cref="FedByComponent"/> that feeds this field; null while the data set is abstract.</summary>
    public string? FedByField { get; set; }

    /// <summary>Gets or sets the discussion on this field, one row per note in <c>data.GraphDataSetFieldNote</c> (FDW-5294), in <see cref="GraphDataSetFieldNoteConfiguration.Ordinal"/> order.</summary>
    public IList<GraphDataSetFieldNoteConfiguration> Notes { get; set; } = [];

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets this field as the shared field type every reader of a data set's configuration takes.</summary>
    /// <returns>A copy of the field as a <see cref="DataSetFieldConfiguration"/>.</returns>
    public DataSetFieldConfiguration ToDataSetField() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        ClassificationId = ClassificationId,
        DatumTypeId = DatumTypeId,
        TypeName = TypeName,
        Role = Role,
        IsKey = IsKey,
        IsNullable = IsNullable,
        IsRequired = IsRequired,
        IsSystemProvided = IsSystemProvided,
        IsIndexed = IsIndexed,
        IsFilterable = IsFilterable,
        MaxLength = MaxLength,
        Ordinal = Ordinal,
        IsJoinKey = IsJoinKey,
        CalculationName = CalculationName,
        IsCurrent = IsCurrent,
        IsDeleted = IsDeleted,
        SrcCreateDate = SrcCreateDate,
        CreateDate = CreateDate,
        CreateBy = CreateBy,
        CreateOnBehalfOf = CreateOnBehalfOf,
        ModifyDate = ModifyDate,
        ModifyBy = ModifyBy,
        ModifyOnBehalfOf = ModifyOnBehalfOf
    };
}

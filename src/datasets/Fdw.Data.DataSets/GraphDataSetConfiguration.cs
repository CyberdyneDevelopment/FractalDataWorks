using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The Graph implementation of DataSet: a data set defined by a graph of row components (FDW-5286).</summary>
/// <remarks>
/// Maps to <c>data.GraphDataSetConfiguration</c>, keyed to its domain row in <c>data.DataSet</c>. Its fields out are
/// <see cref="GraphFields"/>, stored in <c>data.GraphDataSetField</c>; its graph is <see cref="Components"/> and <see cref="Links"/>. It has no sources or key fields of its own in the shared
/// sense: the lists below are empty by design, because what it reads is the graph, not a source list.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetConfiguration : IGraphDataSetConfiguration
{
    /// <summary>The implementation name a Graph data set's domain row carries.</summary>
    public const string ImplementationName = "Graph";

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the abbreviation.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets whether a reader may sort its rows.</summary>
    public bool IsSortable { get; set; }

    /// <summary>Gets or sets whether a reader may filter its rows.</summary>
    public bool IsFilterable { get; set; }

    /// <summary>Gets or sets the source data set name; a Graph data set has none, its sources are in its graph.</summary>
    [NotStored]
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets how sensitive this data set's data is: the <c>ClassificationId</c> of a <c>DataClassifications</c> level.</summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the logical id of the user who created the data set; null when it has no owner.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the component that carries the fields out; null while the data set is abstract. Set with <see cref="FieldsOutOutput"/>.</summary>
    public string? FieldsOutComponent { get; set; }

    /// <summary>Gets or sets the name of the output node of <see cref="FieldsOutComponent"/> that carries the fields out; null while the data set is abstract.</summary>
    public string? FieldsOutOutput { get; set; }

    /// <summary>Gets or sets the fields out as they are stored.</summary>
    public IList<GraphDataSetFieldConfiguration> GraphFields { get; set; } = new List<GraphDataSetFieldConfiguration>();

    /// <summary>Gets or sets the components of the graph. Cascade children (<c>data.GraphDataSetComponent</c>); none while the data set is abstract.</summary>
    public IList<GraphDataSetComponentConfiguration> Components { get; set; } = new List<GraphDataSetComponentConfiguration>();

    /// <summary>Gets or sets the links of the graph. Cascade children (<c>data.GraphDataSetLink</c>); none while the data set is abstract.</summary>
    public IList<GraphDataSetLinkConfiguration> Links { get; set; } = new List<GraphDataSetLinkConfiguration>();

    /// <summary>Gets or sets the intended sources, one row per intent in <c>data.GraphDataSetIntent</c> (FDW-5294); empty once the data set is bound.</summary>
    public IList<GraphDataSetIntentConfiguration> Intents { get; set; } = new List<GraphDataSetIntentConfiguration>();

    /// <summary>Gets or sets the fields out as the shared field type; a read returns a copy of <see cref="GraphFields"/>.</summary>
    /// <remarks>
    /// Not stored: <see cref="GraphFields"/> is the stored list. Setting this replaces <see cref="GraphFields"/> with the given
    /// fields, so a change to the list a getter returned does not reach the data set. A field of the same name keeps what the shared type has no member for:
    /// what feeds it and its notes.
    /// </remarks>
    [NotStored]
    public IList<DataSetFieldConfiguration> Fields
    {
        get => [.. GraphFields.Select(stored => stored.ToDataSetField())];
        set
        {
            var stored = GraphFields.GroupBy(row => row.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            GraphFields = [.. value.Select(given => FromShared(given, stored.GetValueOrDefault(given.Name)))];
        }
    }

    /// <summary>Gets or sets the sources; a Graph data set has none in the shared sense, what it reads is in its graph.</summary>
    [NotStored]
    public IList<DataSetSourceConfiguration> Sources { get; set; } = new List<DataSetSourceConfiguration>();

    /// <summary>Gets or sets the caching.</summary>
    public CachingConfiguration? Caching { get; set; }

    /// <summary>Gets or sets the filter conditions every data set carries; a Graph data set's filters are components of its graph.</summary>
    [NotStored]
    public IList<DataSetFilterConditionConfiguration> Filters { get; set; } = new List<DataSetFilterConditionConfiguration>();

    /// <summary>Gets or sets the key fields; a Graph data set has none of its own in the shared sense.</summary>
    [NotStored]
    public IList<DataSetKeyFieldConfiguration> KeyFields { get; set; } = new List<DataSetKeyFieldConfiguration>();

    /// <summary>Gets the durable ids of this data set's sources.</summary>
    public IReadOnlyList<Guid> SourceIds => Sources.Select(s => s.Id).ToList();

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;

    // The stored field out for a field of the shared type. What the shared type has no member for (what feeds the field, and its discussion) is kept from the
    // stored field of the same name, so a route that knows only the shared type does not drop it silently; a new field has none.
    private static GraphDataSetFieldConfiguration FromShared(DataSetFieldConfiguration given, GraphDataSetFieldConfiguration? kept) => new()
    {
        Id = given.Id,
        Name = given.Name,
        Description = given.Description,
        ClassificationId = given.ClassificationId,
        DatumTypeId = given.DatumTypeId,
        TypeName = given.TypeName,
        Role = given.Role,
        IsKey = given.IsKey,
        IsNullable = given.IsNullable,
        IsRequired = given.IsRequired,
        IsSystemProvided = given.IsSystemProvided,
        IsIndexed = given.IsIndexed,
        IsFilterable = given.IsFilterable,
        MaxLength = given.MaxLength,
        Ordinal = given.Ordinal,
        IsJoinKey = given.IsJoinKey,
        CalculationName = given.CalculationName,
        IsCurrent = given.IsCurrent,
        IsDeleted = given.IsDeleted,
        SrcCreateDate = given.SrcCreateDate,
        CreateDate = given.CreateDate,
        CreateBy = given.CreateBy,
        CreateOnBehalfOf = given.CreateOnBehalfOf,
        ModifyDate = given.ModifyDate,
        ModifyBy = given.ModifyBy,
        ModifyOnBehalfOf = given.ModifyOnBehalfOf,
        FedByComponent = kept?.FedByComponent,
        FedByField = kept?.FedByField,
        Notes = [.. kept?.Notes ?? []],
    };
}

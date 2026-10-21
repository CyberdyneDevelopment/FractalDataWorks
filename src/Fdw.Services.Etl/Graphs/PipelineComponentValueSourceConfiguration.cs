using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// A value a destination supplies for one target column instead of, or as well as, the column its rows carry (FDW-1636): a source
/// field, the next value of a sequence, the current time or a constant.
/// </summary>
/// <remarks>
/// <para>
/// It is the value source of the old load step, now owned by the destination component. The rules for each kind (which fields it
/// needs and which it refuses) are the ones <c>ValueSourceKinds</c> states and the load validator checks.
/// </para>
/// <para>Cascade child of a component, in <c>pipe.PipelineComponentValueSource</c>.</para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentValueSourceConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the position of the value source among the component's, the order they were stated in.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the target column the value is written to.</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>Gets or sets how the value is made, a <c>ValueSourceKinds</c> option name.</summary>
    [ValuesFrom(typeof(ValueSourceKinds))]
    public string SourceKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the field of the received rows a Column value is taken from.</summary>
    public string? SourceField { get; set; }

    /// <summary>Gets or sets the sequence a NextSequenceValue value is drawn from.</summary>
    public string? SequenceName { get; set; }

    /// <summary>Gets or sets the schema of the sequence.</summary>
    public string? SequenceSchema { get; set; }

    /// <summary>Gets or sets the database of the sequence.</summary>
    public string? SequenceDatabase { get; set; }

    /// <summary>Gets or sets a Constant value as text.</summary>
    public string? ConstantValue { get; set; }

    /// <summary>Gets or sets the <c>DataTypes</c> option that reads <see cref="ConstantValue"/> back.</summary>
    [ValuesFrom(typeof(DataTypes))]
    public string? ConstantDataType { get; set; }

    /// <summary>Gets or sets the columns a sequenced value is numbered in the order of. Cascade children (<c>pipe.PipelineComponentValueSourceColumn</c>).</summary>
    public IList<PipelineComponentValueSourceColumnConfiguration> OrderColumns { get; set; } = [];
}

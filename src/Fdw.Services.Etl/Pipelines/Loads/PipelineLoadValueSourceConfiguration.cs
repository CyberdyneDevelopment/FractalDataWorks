using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// A value a load supplies for one target column: a field of the source row, the next value of a sequence, the
/// current time or a constant (FDW-1108). On an Execute step it is one parameter of the executable container, and
/// <see cref="ColumnName"/> holds the parameter's name. The graph destination builders make these in memory for
/// <see cref="PipelineLoadStepConfiguration"/>; nothing stores one.
/// </summary>
/// <remarks>
/// <see cref="Name"/> carries the column's name.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PipelineLoadValueSourceConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display and binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target column the value is written to, or the parameter name on an Execute step.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of value, resolved against <see cref="ValueSourceKinds"/>.
    /// </summary>
    [ValuesFrom(typeof(ValueSourceKinds))]
    public string SourceKind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field of the source row to read (the Column kind), or <see langword="null"/>.
    /// </summary>
    public string? SourceField { get; set; }

    /// <summary>
    /// Gets or sets the name of the sequence (kinds that need one), or <see langword="null"/>.
    /// </summary>
    public string? SequenceName { get; set; }

    /// <summary>
    /// Gets or sets the optional schema that holds the sequence, or <see langword="null"/>. Only with a sequence name.
    /// </summary>
    public string? SequenceSchema { get; set; }

    /// <summary>
    /// Gets or sets the optional database that holds the sequence, or <see langword="null"/>. Only with a sequence name.
    /// </summary>
    public string? SequenceDatabase { get; set; }

    /// <summary>
    /// Gets or sets the constant, written as text (the Constant kind), or <see langword="null"/>.
    /// </summary>
    public string? ConstantValue { get; set; }

    /// <summary>
    /// Gets or sets the type that reads <see cref="ConstantValue"/> back, resolved against <see cref="DataTypes"/>, or <see langword="null"/>.
    /// </summary>
    [ValuesFrom(typeof(DataTypes))]
    public string? ConstantDataType { get; set; }

    /// <summary>
    /// Gets or sets the ordering columns of a sequenced value, ordered by their ordinal. Rows live in <c>pipe.PipelineLoadColumn</c>, each holding this row's key in <c>PipelineLoadValueSourceRowId</c>.
    /// </summary>
    public IList<PipelineLoadColumnConfiguration> SequenceOrderColumns { get; set; } = [];
}

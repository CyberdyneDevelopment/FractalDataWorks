using System;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>A constant, stored as text with the type that reads it.</summary>
[TypeOption(typeof(ValueSourceFactories), "Constant")]
public sealed class ConstantValueSourceFactory : ValueSourceFactoryBase
{
    /// <summary>Initializes a new instance of the <see cref="ConstantValueSourceFactory"/> class.</summary>
    public ConstantValueSourceFactory() : base("Constant", suppliesRowValue: true)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<ColumnValueSource> Create(PipelineLoadValueSourceConfiguration source, string owner, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(source.ConstantDataType) || source.ConstantValue is null)
        {
            return GenericResult<ColumnValueSource>.Failure(
                PipelineLoadLog.ValueSourceOptionMissing(logger, owner, source.ColumnName, "a constant value and the type that reads it"));
        }

        var type = DataTypes.ByName(source.ConstantDataType);
        var value = ReferenceEquals(type, DataTypes.NotFound) ? null : type.Convert(source.ConstantValue);
        if (value is null || value.IsFailure)
        {
            return GenericResult<ColumnValueSource>.Failure(
                PipelineRunLog.ConstantUnreadable(logger, owner, source.ColumnName, source.ConstantDataType));
        }

        return GenericResult<ColumnValueSource>.Success(new ConstantValueSource(value.Value));
    }

    /// <inheritdoc/>
    public override object? RowValue(ColumnValueSource source, DateTimeOffset now) => ((ConstantValueSource)source).Value;
}

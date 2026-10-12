using System;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>The current time: the database clock when the command supplies it, the moment the load began when the rows do.</summary>
[TypeOption(typeof(ValueSourceFactories), "CurrentTimestamp")]
public sealed class CurrentTimestampValueSourceFactory : ValueSourceFactoryBase
{
    /// <summary>Initializes a new instance of the <see cref="CurrentTimestampValueSourceFactory"/> class.</summary>
    public CurrentTimestampValueSourceFactory() : base("CurrentTimestamp", suppliesRowValue: true)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<ColumnValueSource> Create(PipelineLoadValueSourceConfiguration source, string owner, ILogger logger)
        => GenericResult<ColumnValueSource>.Success(new CurrentTimestampValueSource());

    /// <inheritdoc/>
    public override object? RowValue(ColumnValueSource source, DateTimeOffset now) => now;
}

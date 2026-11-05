using System;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>
/// A new UUIDv7 for each row written: minted by the engine for each row of an insert, rendered by the connection type for the rows an upsert
/// inserts. It is never given to a row that already exists, so an update load refuses it.
/// </summary>
[TypeOption(typeof(ValueSourceFactories), "UuidV7")]
public sealed class UuidV7ValueSourceFactory : ValueSourceFactoryBase
{
    /// <summary>Initializes a new instance of the <see cref="UuidV7ValueSourceFactory"/> class.</summary>
    public UuidV7ValueSourceFactory() : base("UuidV7", suppliesRowValue: true, insertOnly: true)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<ColumnValueSource> Create(PipelineLoadValueSourceConfiguration source, string owner, ILogger logger)
        => GenericResult<ColumnValueSource>.Success(new UuidV7ValueSource());

    /// <inheritdoc/>
    public override Func<object?> RowValueProducer(ColumnValueSource source, DateTimeOffset now) => () => Guid.CreateVersion7();
}

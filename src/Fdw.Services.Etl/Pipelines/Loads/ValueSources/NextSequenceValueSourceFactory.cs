using System;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>The next value of a sequence, numbering a batch in the order of the ordering columns (set-based loads only).</summary>
[TypeOption(typeof(ValueSourceFactories), "NextSequenceValue")]
public sealed class NextSequenceValueSourceFactory : ValueSourceFactoryBase
{
    /// <summary>Initializes a new instance of the <see cref="NextSequenceValueSourceFactory"/> class.</summary>
    public NextSequenceValueSourceFactory() : base("NextSequenceValue", suppliesRowValue: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<ColumnValueSource> Create(PipelineLoadValueSourceConfiguration source, string owner, ILogger logger)
        => string.IsNullOrWhiteSpace(source.SequenceName)
            ? GenericResult<ColumnValueSource>.Failure(PipelineLoadLog.ValueSourceOptionMissing(logger, owner, source.ColumnName, "a sequence name"))
            : GenericResult<ColumnValueSource>.Success(new NextSequenceValueSource(
                source.SequenceName, LoadColumns.SequenceOrder(source), source.SequenceSchema, source.SequenceDatabase));
}

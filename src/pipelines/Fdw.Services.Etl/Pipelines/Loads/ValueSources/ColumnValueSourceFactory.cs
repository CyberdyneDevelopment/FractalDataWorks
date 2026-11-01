using Fdw.Collections.Attributes;
using Fdw.Commands.Data.ValueSources;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>A destination column that takes a source field of another name (insert-from-query only).</summary>
[TypeOption(typeof(ValueSourceFactories), "Column")]
public sealed class ColumnValueSourceFactory : ValueSourceFactoryBase
{
    /// <summary>Initializes a new instance of the <see cref="ColumnValueSourceFactory"/> class.</summary>
    public ColumnValueSourceFactory() : base("Column", suppliesRowValue: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<ColumnValueSource> Create(PipelineLoadValueSourceConfiguration source, string owner, ILogger logger)
        => string.IsNullOrWhiteSpace(source.SourceField)
            ? GenericResult<ColumnValueSource>.Failure(PipelineLoadLog.ValueSourceOptionMissing(logger, owner, source.ColumnName, "a source field"))
            : GenericResult<ColumnValueSource>.Success(new ColumnReferenceValueSource(source.SourceField));
}

using System;
using Fdw.Collections;
using Fdw.Commands.Data.ValueSources;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>
/// Builds the command-layer value source of one kind from its configuration row (FDW-1109). One option per
/// <c>ValueSourceKinds</c> option, found by the kind's name, so no caller branches on a kind.
/// </summary>
public interface IValueSourceFactory : ITypeOption<int, IValueSourceFactory>
{
    /// <summary>Gets whether the engine can give a column its value in the rows themselves, before a load that takes rows.</summary>
    bool SuppliesRowValue { get; }

    /// <summary>Builds the value source of this kind from its row.</summary>
    /// <param name="source">The configuration row.</param>
    /// <param name="owner">What the value source belongs to, for refusal messages.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>The value source, or a failure saying why the row cannot be built.</returns>
    IGenericResult<ColumnValueSource> Create(PipelineLoadValueSourceConfiguration source, string owner, ILogger logger);

    /// <summary>Gives the value a row-supplying kind puts in its column.</summary>
    /// <param name="source">The built value source.</param>
    /// <param name="now">The moment the load began, the same for every row.</param>
    /// <returns>The value.</returns>
    object? RowValue(ColumnValueSource source, DateTimeOffset now);
}

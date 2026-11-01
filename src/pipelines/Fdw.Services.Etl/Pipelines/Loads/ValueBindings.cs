using System;
using System.Collections.Generic;
using Fdw.Commands.Data.ValueSources;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads.ValueSources;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Turns the value sources of a load row into command-layer bindings, and into the values a load that takes rows gives its
/// rows itself (FDW-1109).
/// </summary>
public static class ValueBindings
{
    /// <summary>Builds one binding per value source, in the order stated.</summary>
    /// <param name="sources">The value source rows.</param>
    /// <param name="owner">What they belong to, for refusal messages.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>The bindings, or the first refusal.</returns>
    public static IGenericResult<IReadOnlyList<ColumnValueBinding>> Build(
        IList<PipelineLoadValueSourceConfiguration> sources,
        string owner,
        ILogger logger)
    {
        var bindings = new List<ColumnValueBinding>(sources.Count);
        foreach (var source in sources)
        {
            var factory = ValueSourceFactories.ByName(source.SourceKind);
            if (ReferenceEquals(factory, ValueSourceFactories.NotFound))
                return GenericResult<IReadOnlyList<ColumnValueBinding>>.Failure(
                    PipelineRunLog.ValueSourceKindUnbuildable(logger, owner, source.ColumnName, source.SourceKind));

            var built = factory.Create(source, owner, logger);
            if (built.IsFailure)
                return built.ToNewResult<IReadOnlyList<ColumnValueBinding>>();
            bindings.Add(new ColumnValueBinding(source.ColumnName, built.Value!));
        }

        return GenericResult<IReadOnlyList<ColumnValueBinding>>.Success(bindings);
    }

    /// <summary>
    /// Gives each binding the value it puts in the rows of a load that takes rows (Insert, Update). A kind that needs
    /// the database to number or stamp the rows set-based cannot be given to rows, and is refused.
    /// </summary>
    /// <param name="bindings">The bindings.</param>
    /// <param name="owner">What they belong to, for refusal messages.</param>
    /// <param name="loadKind">The kind of load, for refusal messages.</param>
    /// <param name="now">The moment the load began, the same for every row.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Column and value pairs, or the first refusal.</returns>
    public static IGenericResult<IReadOnlyList<KeyValuePair<string, object?>>> RowValues(
        IReadOnlyList<ColumnValueBinding> bindings,
        string owner,
        string loadKind,
        DateTimeOffset now,
        ILogger logger)
    {
        var values = new List<KeyValuePair<string, object?>>(bindings.Count);
        foreach (var binding in bindings)
        {
            var factory = ValueSourceFactories.ByName(binding.Source.Kind);
            if (ReferenceEquals(factory, ValueSourceFactories.NotFound) || !factory.SuppliesRowValue)
                return GenericResult<IReadOnlyList<KeyValuePair<string, object?>>>.Failure(
                    PipelineRunLog.ValueSourceNotForLoad(logger, owner, binding.Column, binding.Source.Kind, loadKind));
            values.Add(new KeyValuePair<string, object?>(binding.Column, factory.RowValue(binding.Source, now)));
        }

        return GenericResult<IReadOnlyList<KeyValuePair<string, object?>>>.Success(values);
    }
}

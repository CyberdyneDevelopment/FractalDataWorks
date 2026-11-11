using System;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The one rule that says a Compound data set's field mappings name every column its joined query reads (FDW-5022): checked when the
/// data set is created or updated, and again when it is read.
/// </summary>
/// <remarks>
/// A Compound data set reads every stored field from the one source that maps it and joins on columns its sources map. A field no source
/// maps, a field two sources map, or a join column its source does not map would make the read guess which source or which column it
/// meant. Nothing is guessed (the no-fallbacks rule: "No tree-walking guesser. No 'I'll figure out which key' picker"), so each is refused
/// with its own code, naming the field and the source.
/// </remarks>
public static class CompoundMappingRules
{
    /// <summary>Checks that every stored field is mapped by exactly one source and every join column by its own source.</summary>
    /// <param name="dataSet">The Compound data set, as it will be saved or as it was read.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// Success, or the first refusal: <c>JoinSourceNotInDataSet</c> (<see cref="JoinedDataSetRules"/>), <c>CompoundFieldNotMapped</c>,
    /// <c>CompoundFieldMappedTwice</c> or <c>CompoundJoinColumnNotMapped</c>.
    /// </returns>
    public static IGenericResult Validate(ICompoundDataSetConfiguration dataSet, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        ArgumentNullException.ThrowIfNull(logger);

        var sources = JoinedDataSetRules.Validate(dataSet, logger);
        if (sources.IsFailure)
            return sources;

        var fields = Fields(dataSet, logger);
        return fields.IsFailure ? fields : Joins(dataSet, logger);
    }

    private static IGenericResult Fields(ICompoundDataSetConfiguration dataSet, ILogger logger)
    {
        foreach (var field in dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted && !f.IsCalculated))
        {
            var mappedBy = dataSet.Sources.Where(source => source.FieldMappings.ContainsKey(field.Name)).Select(source => source.SourceName).ToList();
            if (mappedBy.Count == 0)
                return GenericResult.Failure(
                    DataSetsResultCodes.ByName("CompoundFieldNotMapped"), logger,
                    ResultDetails.Create("name", dataSet.Name, "field", field.Name, "sources", string.Join(", ", dataSet.Sources.Select(s => s.SourceName))));

            if (mappedBy.Count > 1)
                return GenericResult.Failure(
                    DataSetsResultCodes.ByName("CompoundFieldMappedTwice"), logger,
                    ResultDetails.Create("name", dataSet.Name, "field", field.Name, "firstSource", mappedBy[0]).With("secondSource", mappedBy[1]));
        }

        return GenericResult.Success();
    }

    // A join whose source is not one of the data set's, or whose type does not join, was refused by JoinedDataSetRules; this checks only the
    // columns, and only of a join on a condition: a join of every row with every row (Cross) reads none (FDW-5099).
    private static IGenericResult Joins(ICompoundDataSetConfiguration dataSet, ILogger logger)
    {
        foreach (var join in dataSet.Joins.Where(candidate => StatedJoinType.Of(candidate.JoinType).RequiresConditions))
        {
            var left = dataSet.Sources.FirstOrDefault(s => string.Equals(s.SourceName, join.LeftSourceName, StringComparison.OrdinalIgnoreCase));
            if (left is not null && !left.FieldMappings.ContainsKey(join.LeftFieldName))
                return JoinColumnNotMapped(dataSet, join, join.LeftSourceName, join.LeftFieldName, logger);

            var right = dataSet.Sources.FirstOrDefault(s => string.Equals(s.SourceName, join.RightSourceName, StringComparison.OrdinalIgnoreCase));
            if (right is not null && !right.FieldMappings.ContainsKey(join.RightFieldName))
                return JoinColumnNotMapped(dataSet, join, join.RightSourceName, join.RightFieldName, logger);
        }

        return GenericResult.Success();
    }

    private static IGenericResult JoinColumnNotMapped(
        ICompoundDataSetConfiguration dataSet, DataSetJoinConfiguration join, string source, string field, ILogger logger) =>
        GenericResult.Failure(
            DataSetsResultCodes.ByName("CompoundJoinColumnNotMapped"), logger,
            ResultDetails.Create("name", dataSet.Name, "leftSource", join.LeftSourceName, "rightSource", join.RightSourceName)
                .With("source", source).With("field", field));
}

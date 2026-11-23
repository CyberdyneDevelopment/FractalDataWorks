using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// The one place a federated read's strategy and options are resolved (FDW-6901), for a Federated data set and a Graph data set alike.
/// </summary>
/// <remarks>
/// The strategy is the data set's own, else the FDW-level default (FederationSettings.DefaultStrategy). Each option is the data set's own value,
/// else the FDW-level value for that strategy, else the value the strategy declares on itself. Nothing is a literal here: every value comes from one
/// of those three places, and each is reported with where it came from.
/// </remarks>
public static class FederationResolution
{
    /// <summary>The option that says whether a strategy reads its sources at the same time.</summary>
    public const string ReadsInParallelOption = FederationStrategyDefaults.ReadsInParallel;

    /// <summary>The option that says how many sources a strategy reads at once when it reads in parallel.</summary>
    public const string MaxParallelReadsOption = FederationStrategyDefaults.MaxParallelReads;

    /// <summary>The option that says whether a strategy reads its sources in their stated Priority order.</summary>
    public const string OrdersSourcesOption = FederationStrategyDefaults.OrdersSources;

    private static readonly string[] OptionNames = [ReadsInParallelOption, MaxParallelReadsOption, OrdersSourcesOption];

    /// <summary>Resolves the strategy and options a read of a data set runs under.</summary>
    /// <param name="settings">The FDW-level settings; null when the host registers none.</param>
    /// <param name="dataSetName">The data set's name, for a refusal and the trace.</param>
    /// <param name="dataSetStrategy">The strategy the data set states, or null.</param>
    /// <param name="dataSetOptions">The data set's own option values.</param>
    /// <param name="logger">The logger a refusal and the trace are written to.</param>
    /// <returns>
    /// The resolved federation, or the refusal: no strategy stated and none configured, a strategy that is no FederationStrategies member, an
    /// option name the strategy does not have, or a value that does not parse (or a read limit below one).
    /// </returns>
    public static IGenericResult<ResolvedFederation> Resolve(
        FederationSettings? settings,
        string dataSetName,
        string? dataSetStrategy,
        IReadOnlyList<DataSetFederationOptionConfiguration> dataSetOptions,
        ILogger logger)
    {
        var statedByDataSet = !string.IsNullOrWhiteSpace(dataSetStrategy);
        var name = statedByDataSet ? dataSetStrategy : settings?.DefaultStrategy;
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<ResolvedFederation>.Failure(
                DataGatewayLogger.FederationStrategyMissing(logger, dataSetName, "the data set states no FederationStrategy and no FederationSettings are registered"));

        var strategy = FederationStrategies.ByName(name);
        if (ReferenceEquals(strategy, FederationStrategies.NotFound))
            return GenericResult<ResolvedFederation>.Failure(
                DataGatewayLogger.FederationStrategyMissing(logger, dataSetName, $"'{name}' is not a registered FederationStrategies member"));

        var own = dataSetOptions.Where(option => option.IsCurrent && !option.IsDeleted).ToList();
        if (own.FirstOrDefault(option => !OptionNames.Contains(option.OptionName, StringComparer.OrdinalIgnoreCase)) is { } unknown)
            return GenericResult<ResolvedFederation>.Failure(
                DataGatewayLogger.FederationOptionUnknown(logger, dataSetName, unknown.OptionName, name, string.Join(", ", OptionNames)));

        var fdw = settings is not null && settings.Strategies.TryGetValue(name, out var stated) ? stated : null;
        var parallel = Bool(own, ReadsInParallelOption, fdw?.ReadsInParallel, strategy.IsParallel, dataSetName, logger);
        if (parallel.IsFailure) return parallel.ToNewResult<ResolvedFederation>();
        var limit = Int(own, MaxParallelReadsOption, fdw?.MaxParallelReads, strategy.MaxParallelReads, dataSetName, logger);
        if (limit.IsFailure) return limit.ToNewResult<ResolvedFederation>();
        var ordered = Bool(own, OrdersSourcesOption, fdw?.OrdersSources, strategy.OptimizesOrder, dataSetName, logger);
        if (ordered.IsFailure) return ordered.ToNewResult<ResolvedFederation>();

        var resolved = new ResolvedFederation(
            name,
            statedByDataSet ? FederationValueSources.DataSet : FederationValueSources.Configuration,
            parallel.Value.Value,
            parallel.Value.From,
            limit.Value.Value,
            limit.Value.From,
            ordered.Value.Value,
            ordered.Value.From);
        DataGatewayLogger.FederationOptionsResolved(logger, dataSetName, resolved.Strategy, resolved.ReadsInParallel, resolved.MaxParallelReads, resolved.OrdersSources);
        return GenericResult<ResolvedFederation>.Success(resolved);
    }

    /// <summary>
    /// Checks a data set's own option values when it is saved (FDW-6901): each names an option a strategy has, at most once, and its value parses as
    /// that option's type (a read limit of at least one). The same check every read makes, so a value that would refuse a read is never stored.
    /// </summary>
    /// <param name="dataSetName">The data set's name, for a refusal.</param>
    /// <param name="dataSetOptions">The values as they will be saved.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the first refusal (DATA-21207, DATA-21208).</returns>
    public static IGenericResult Check(string dataSetName, IReadOnlyList<DataSetFederationOptionConfiguration> dataSetOptions, ILogger logger)
    {
        var own = dataSetOptions.Where(option => option.IsCurrent && !option.IsDeleted).ToList();
        foreach (var option in own)
        {
            if (!OptionNames.Contains(option.OptionName, StringComparer.OrdinalIgnoreCase)
                || own.Count(other => string.Equals(other.OptionName, option.OptionName, StringComparison.OrdinalIgnoreCase)) > 1)
                return GenericResult.Failure(
                    DataGatewayLogger.FederationOptionUnknown(logger, dataSetName, option.OptionName, "any strategy (once each)", string.Join(", ", OptionNames)));
        }

        var unparsable = own.FirstOrDefault(option => !Parses(option));
        return unparsable is null
            ? GenericResult.Success()
            : GenericResult.Failure(DataGatewayLogger.FederationOptionInvalid(
                logger, dataSetName, unparsable.OptionName, unparsable.OptionValue, Expected(unparsable.OptionName)));
    }

    // Whether a stated value parses as its option's type: a whole number of at least one for the read limit, true or false for the others.
    private static bool Parses(DataSetFederationOptionConfiguration option) =>
        string.Equals(option.OptionName, MaxParallelReadsOption, StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(option.OptionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit) && limit >= 1
            : bool.TryParse(option.OptionValue, out _);

    private static string Expected(string option) =>
        string.Equals(option, MaxParallelReadsOption, StringComparison.OrdinalIgnoreCase) ? "a whole number of at least one" : "true or false";

    // A yes/no option: the data set's value, else the FDW-level one, else the declared one.
    private static IGenericResult<(bool Value, IFederationValueSource From)> Bool(
        List<DataSetFederationOptionConfiguration> own, string option, bool? fdw, bool declared, string dataSetName, ILogger logger)
    {
        if (Own(own, option) is { } text)
        {
            return bool.TryParse(text, out var value)
                ? GenericResult<(bool, IFederationValueSource)>.Success((value, FederationValueSources.DataSet))
                : GenericResult<(bool, IFederationValueSource)>.Failure(
                    DataGatewayLogger.FederationOptionInvalid(logger, dataSetName, option, text, "true or false"));
        }

        return GenericResult<(bool, IFederationValueSource)>.Success(
            fdw is { } configured ? (configured, FederationValueSources.Configuration) : (declared, FederationValueSources.Declared));
    }

    // A whole-number option of at least one: the data set's value, else the FDW-level one, else the declared one.
    private static IGenericResult<(int Value, IFederationValueSource From)> Int(
        List<DataSetFederationOptionConfiguration> own, string option, int? fdw, int declared, string dataSetName, ILogger logger)
    {
        if (Own(own, option) is { } text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 1
                ? GenericResult<(int, IFederationValueSource)>.Success((value, FederationValueSources.DataSet))
                : GenericResult<(int, IFederationValueSource)>.Failure(
                    DataGatewayLogger.FederationOptionInvalid(logger, dataSetName, option, text, "a whole number of at least one"));
        }

        return GenericResult<(int, IFederationValueSource)>.Success(
            fdw is { } configured ? (configured, FederationValueSources.Configuration) : (declared, FederationValueSources.Declared));
    }

    private static string? Own(List<DataSetFederationOptionConfiguration> own, string option) =>
        own.FirstOrDefault(row => string.Equals(row.OptionName, option, StringComparison.OrdinalIgnoreCase))?.OptionValue;
}

using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Resolves the FDW-level federation settings when the data services register (FDW-6901), through the one declared-defaults mechanism
/// (<see cref="DeclaredSetting"/>): the value the host's configuration states wins, else the host's <see cref="ServiceTypeDefaults.Override"/>,
/// else the value the option declares.
/// </summary>
/// <remarks>
/// <para>The keys, under <see cref="Section"/>:</para>
/// <list type="bullet">
/// <item><c>DefaultStrategy</c>: the <c>FederationStrategies</c> member a Federated or Graph data set that states none reads under. Declared on
/// the Federated data set kind (<see cref="DataSetDeclaredDefaults.FederationDefaultStrategy"/>, Optimized).</item>
/// <item><c>Strategies:&lt;strategy&gt;:ReadsInParallel</c>, <c>MaxParallelReads</c> and <c>OrdersSources</c>: one strategy's values. Declared
/// on each strategy (<see cref="FederationStrategyDefaults"/>): Sequential reads one source at a time in declared order; Parallel reads at most
/// 4 at once; Optimized reads at most 4 at once in Priority order.</item>
/// </list>
/// <para>
/// A data set's own value wins over all of these. A stated value that cannot be used refuses registration and names its key: a true/false
/// value that is neither and a read limit that is not a whole number of at least one (the mechanism's own codes), and a default strategy that
/// is not a <c>FederationStrategies</c> member, a strategy under <c>Strategies</c> that is not one, or an option a strategy does not have
/// (DATA-21209). None is skipped or read as another value.
/// </para>
/// </remarks>
public static class FederationSettingsConfiguration
{
    /// <summary>The configuration section a host states the federation settings in.</summary>
    public const string Section = "DataStores:Federation";

    private const string StrategiesKey = "Strategies";

    private static readonly string[] Options =
        [FederationStrategyDefaults.ReadsInParallel, FederationStrategyDefaults.MaxParallelReads, FederationStrategyDefaults.OrdersSources];

    /// <summary>Resolves the settings for the host being built.</summary>
    /// <param name="builder">The host being built, whose configuration is read.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The settings, every strategy's values resolved; or the refusal naming the key whose value cannot be used.</returns>
    public static IGenericResult<FederationSettings> Read(IHostApplicationBuilder builder, ILogger logger)
    {
        var stated = Unknown(builder, logger);
        if (stated.IsFailure) return stated.ToNewResult<FederationSettings>();

        var defaultStrategy = DeclaredSetting.ConfiguredName(
            builder, "DataStores", DataSetTypes.ByName("Federated"), DataSetDeclaredDefaults.FederationDefaultStrategy, logger);
        if (defaultStrategy.IsFailure || defaultStrategy.Value is null)
            return defaultStrategy.ToNewResult<FederationSettings>();
        if (!IsStrategy(defaultStrategy.Value))
            return GenericResult<FederationSettings>.Failure(
                DataGatewayLogger.FederationSettingInvalid(logger, $"{Section}:DefaultStrategy", defaultStrategy.Value, $"a federation strategy ({Names()})"));

        var strategies = new Dictionary<string, FederationStrategySettings>(StringComparer.OrdinalIgnoreCase);
        foreach (var strategy in FederationStrategies.All())
        {
            var resolved = Strategy(builder, strategy, logger);
            if (resolved.IsFailure || resolved.Value is null)
                return resolved.ToNewResult<FederationSettings>();
            strategies[strategy.Name] = resolved.Value;
        }

        return GenericResult<FederationSettings>.Success(new FederationSettings(defaultStrategy.Value, strategies));
    }

    // One strategy's values: each the configuration's, else the host's override, else the strategy's declared one.
    private static IGenericResult<FederationStrategySettings> Strategy(IHostApplicationBuilder builder, IFederationStrategy strategy, ILogger logger)
    {
        var section = $"{Section}:{StrategiesKey}:{strategy.Name}";
        var parallel = DeclaredSetting.ConfiguredFlag(builder, section, strategy, FederationStrategyDefaults.ReadsInParallel, logger);
        if (parallel.IsFailure) return parallel.ToNewResult<FederationStrategySettings>();
        var limit = DeclaredSetting.ConfiguredCount(builder, section, strategy, FederationStrategyDefaults.MaxParallelReads, logger);
        if (limit.IsFailure) return limit.ToNewResult<FederationStrategySettings>();
        var ordered = DeclaredSetting.ConfiguredFlag(builder, section, strategy, FederationStrategyDefaults.OrdersSources, logger);
        if (ordered.IsFailure) return ordered.ToNewResult<FederationStrategySettings>();

        return GenericResult<FederationStrategySettings>.Success(new FederationStrategySettings(parallel.Value, limit.Value, ordered.Value));
    }

    // A strategy or an option under Strategies the configuration states that is not one: refused, so a misspelt key never reads as "states none".
    private static IGenericResult Unknown(IHostApplicationBuilder builder, ILogger logger)
    {
        foreach (var strategy in builder.Configuration.GetSection($"{Section}:{StrategiesKey}").GetChildren())
        {
            var key = $"{Section}:{StrategiesKey}:{strategy.Key}";
            if (!IsStrategy(strategy.Key))
                return GenericResult.Failure(DataGatewayLogger.FederationSettingInvalid(logger, key, strategy.Key, $"a federation strategy ({Names()})"));

            if (strategy.GetChildren().FirstOrDefault(option => !Options.Contains(option.Key, StringComparer.OrdinalIgnoreCase)) is { } option)
                return GenericResult.Failure(DataGatewayLogger.FederationSettingInvalid(
                    logger, $"{key}:{option.Key}", option.Value ?? string.Empty, $"one of {string.Join(", ", Options)}"));
        }

        return GenericResult.Success();
    }

    private static bool IsStrategy(string name) => !ReferenceEquals(FederationStrategies.ByName(name), FederationStrategies.NotFound);

    private static string Names() => string.Join(", ", FederationStrategies.All().Select(strategy => strategy.Name));
}

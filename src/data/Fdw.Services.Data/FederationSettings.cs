using System;
using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>
/// The FDW-level settings of federated reads: which federation strategy a Federated data set that states none reads under (FDW-6806), and each
/// strategy's option values (FDW-6901). A data set's own strategy and values win over them; a value left unstated here is the strategy's own.
/// </summary>
/// <remarks>
/// A configuration setting, never a literal in a read (Mike, 2026-11-18: "The default should be configurable, the optimization should be
/// configurable, especially at the FDW level"). The shipped value is stated where the data services are registered (DataStoreServiceTypes), and a
/// host that registers its own settings first replaces it. What each strategy does is its own configuration (FDW-6901).
/// </remarks>
public sealed class FederationSettings
{
    /// <summary>Initializes a new instance of the <see cref="FederationSettings"/> class.</summary>
    /// <param name="defaultStrategy">The name of the <c>FederationStrategies</c> member a Federated data set that states none reads under.</param>
    /// <param name="strategies">Each strategy's FDW-level option values, by strategy name; a strategy not named states none at this level.</param>
    /// <exception cref="ArgumentException">No strategy is named, or a read limit below one is stated.</exception>
    public FederationSettings(string defaultStrategy, IReadOnlyDictionary<string, FederationStrategySettings> strategies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultStrategy);
        foreach (var (name, settings) in strategies)
        {
            if (settings.MaxParallelReads is < 1)
                throw new ArgumentException($"Strategy '{name}' states MaxParallelReads {settings.MaxParallelReads}; it must be at least one.", nameof(strategies));
        }

        DefaultStrategy = defaultStrategy;
        Strategies = new Dictionary<string, FederationStrategySettings>(strategies, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the name of the strategy a Federated data set that states none reads under.</summary>
    public string DefaultStrategy { get; }

    /// <summary>Gets each strategy's FDW-level option values, by strategy name, ignoring case.</summary>
    public IReadOnlyDictionary<string, FederationStrategySettings> Strategies { get; }
}

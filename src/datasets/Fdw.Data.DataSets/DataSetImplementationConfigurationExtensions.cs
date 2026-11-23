using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Reads what only some kinds of data set have, for code that handles every kind and wants the truth for
/// the kind in hand (FDW-1031).
/// </summary>
/// <remarks>
/// A Simple data set has no joins, and a Simple or Compound one has no federation strategy: not a value
/// that is missing, but a thing the kind does not have. Code that runs one kind casts to that kind's contract
/// instead and fails when the configuration is not of it (<c>DataSetKindMismatch</c>); these are for the code
/// that describes or composes any data set, such as the response of a read or the runtime builder.
/// </remarks>
public static class DataSetImplementationConfigurationExtensions
{
    /// <summary>Gets the data set's joins, or none for a kind that has no joins.</summary>
    /// <param name="configuration">The data set's configuration.</param>
    /// <returns>The joins, in the order they were configured.</returns>
    public static IReadOnlyList<DataSetJoinConfiguration> JoinsOrNone(this IDataSetImplementationConfiguration configuration)
        => configuration is IJoinedDataSetConfiguration joined ? [.. joined.Joins] : [];

    /// <summary>Gets the data set's own aggregates, or none for a kind that has none.</summary>
    /// <param name="configuration">The data set's configuration.</param>
    /// <returns>The aggregates, in the order they were configured.</returns>
    public static IReadOnlyList<DataSetAggregateConfiguration> AggregatesOrNone(this IDataSetImplementationConfiguration configuration)
        => configuration is ISimpleDataSetConfiguration simple ? [.. simple.Aggregates] : [];

    /// <summary>Gets the data set's federation strategy, or null for a kind that is not federated.</summary>
    /// <param name="configuration">The data set's configuration.</param>
    /// <returns>The strategy's name; null when the kind has no strategy, or when a federated data set names none.</returns>
    public static string? FederationStrategyOrNone(this IDataSetImplementationConfiguration configuration)
        => configuration is IFederatedDataSetConfiguration federated ? federated.FederationStrategy : null;

    /// <summary>Gets how the data set's sources combine, or null for a kind that has one source or a data set that states none (FDW-6946).</summary>
    /// <param name="configuration">The data set's configuration.</param>
    /// <returns>The name of the <c>DataSetCompositionTypes</c> member the data set states, or null.</returns>
    public static string? SourceCombinationOrNone(this IDataSetImplementationConfiguration configuration)
        => configuration is IJoinedDataSetConfiguration joined ? joined.SourceCombination : null;

    /// <summary>Gets the data set's own federation option values, or none for a kind that has none (FDW-6901): a Federated or a Graph data set has them.</summary>
    /// <param name="configuration">The data set's configuration.</param>
    /// <returns>The values as stored.</returns>
    public static IReadOnlyList<DataSetFederationOptionConfiguration> FederationOptionsOrNone(this IDataSetImplementationConfiguration configuration)
        => configuration is IFederatedDataSetConfiguration federated ? [.. federated.FederationOptions]
            : configuration is GraphDataSetConfiguration graph ? [.. graph.FederationOptions]
            : [];
}

using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Thin execution context handed to a <see cref="IDataSetType"/> strategy's
/// <see cref="IDataSetType.Execute{T}"/> so the strategy can run a command against the dataset's
/// authored configuration without taking a constructor dependency.
/// </summary>
/// <remarks>
/// Why: dataset strategy type-options (Simple/Compound/Federated) are module-init singletons with a
/// parameterless constructor — they have no DI. Per-call state (the resolved configuration and the
/// providers needed to pull/join sources) flows in through this context instead. This abstraction
/// exposes only what the abstractions layer can name (<see cref="IGenericConfiguration"/>); the
/// concrete implementation in the Services.Data layer adds the connection/data-store providers, and
/// strategies downcast to it. Keeping the rich members out of this interface preserves the layering
/// (abstractions cannot reference the connection/data-store provider implementations).
/// </remarks>
public interface IDataSetExecutionContext
{
    /// <summary>
    /// Gets the resolved dataset configuration this execution runs against (the composed aggregate,
    /// including its sources). Strategies read structure (sources, joins, fields) from here.
    /// </summary>
    IGenericConfiguration Configuration { get; }
}

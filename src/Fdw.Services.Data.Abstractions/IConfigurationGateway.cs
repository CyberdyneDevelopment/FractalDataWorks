using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Gateway variant that targets configuration data. Provides a
/// <see cref="DataStores"/> property that exposes the IDataStore tree built from the bound
/// <c>configurationSchema.json</c>, so that <c>IConfigurationContainerLookup</c> and other
/// consumers can read schema/table metadata without a separate lazy registration.
/// </summary>
public interface IConfigurationGateway : IDataGateway
{
    /// <summary>Gets the configuration connection this gateway reads and writes.</summary>
    /// <remarks>
    /// A gateway carries its own identity so a holder can say which one it has. An endpoint or any
    /// other consumer handed a gateway can name the connection it is on without tracking that
    /// separately from the instance, and <c>IConfigurationGatewayProvider</c> registers by this rather
    /// than by a name a caller supplies alongside.
    /// </remarks>
    string ConnectionName { get; }

    /// <summary>
    /// Gets the IDataStore tree built from the bound <c>ConfigurationSchema</c>.
    /// The configuration-tier tree — containers, fields, and keys are resolved from the JSON
    /// without a database round-trip. Consumed by <c>ConfigurationContainerLookup</c>.
    /// </summary>
    IReadOnlyList<IDataStore> DataStores { get; }

    /// <summary>
    /// Executes a configuration save command that returns no materialized value (INSERT/UPDATE) against
    /// an explicitly identified container. Non-generic counterpart of
    /// <see cref="IDataGateway.Execute{T}(IDataCommand, DataStoreTarget, CancellationToken)"/> used by the
    /// save cascade, whose child type is known only at runtime — so it cannot close <c>Execute&lt;T&gt;</c>
    /// without reflection.
    /// </summary>
    /// <param name="command">The save command to execute.</param>
    /// <param name="target">The DataStore/Path/Container address of the child table.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result (success or structured failure).</returns>
    Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and materializes the result rows as objects of <paramref name="rowType"/> against
    /// an explicitly identified container. Read counterpart of the no-value
    /// <see cref="Execute(IDataCommand, DataStoreTarget, CancellationToken)"/>: used by the provider's
    /// child composition, whose child row type is known only at runtime — so it cannot close
    /// <c>Execute&lt;T&gt;</c> without reflection.
    /// </summary>
    /// <param name="command">The query command to execute.</param>
    /// <param name="target">The DataStore/Path/Container address of the child table.</param>
    /// <param name="rowType">The CLR row type to materialize each result row as.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The materialized rows (as objects) or a structured failure.</returns>
    Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default);
}

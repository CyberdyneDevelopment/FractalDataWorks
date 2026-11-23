using System;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Describes a resolved container for a planner (FDW-6969): the one place both data gateways build a <see cref="PushdownContainer"/>, so the
/// standard gateway and the ETL server's gateway say the same thing about the same container.
/// </summary>
public static class PushdownContainers
{
    /// <summary>Describes a container.</summary>
    /// <param name="dataStoreId">The id of the data store's configuration record.</param>
    /// <param name="container">The container, resolved as for any command.</param>
    /// <param name="connection">The connection its store reaches it by.</param>
    /// <returns>The description.</returns>
    /// <remarks>
    /// The fields returned are those a read with no projection returns (<see cref="IContainerSchema.GetReturnedFields"/> with nothing included),
    /// which is what a source component that reads the container sends on: so a pushed query projects exactly the fields the in-memory read
    /// would have carried. A joined statement names each joined table and qualifies its columns by the container's name, so a container is
    /// pushed only when that name is the name it is stored under.
    /// </remarks>
    public static PushdownContainer Of(Guid dataStoreId, IDataContainer container, IDataConnection connection) => new(
        dataStoreId,
        connection.JoinsInStore,
        container.Path is IDatabasePath path && string.Equals(path.ObjectName, container.Name, StringComparison.Ordinal),
        [.. container.Schema.GetReturnedFields(false, false, false).Select(field => new PushdownField(field.Name, field.ExplicitType?.ClrType))],
        container.Keys.FirstOrDefault(key => key.KeyType.IsPrimaryKey && key.KeyFields.Count > 0) is { } primary
            ? [.. primary.KeyFields.Select(field => field.LocalField.Name)]
            : []);
}

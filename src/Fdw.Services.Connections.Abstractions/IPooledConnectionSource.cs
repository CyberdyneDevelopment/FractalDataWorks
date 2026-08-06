using System.Data.Common;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A connection that can hand out an unopened pooled provider connection for callers that need raw
/// ADO rather than the command/translator path.
/// </summary>
/// <remarks>
/// Why this exists: schema discovery and the data vaults are backend-specific ADO that lives in the
/// framework, while the connection implementations they run against are reference packages. Taking
/// the concrete connection as a parameter pinned the framework to one backend's implementation
/// package and required a connection-type test — exactly what the "connection type is invisible
/// above the connection layer" rule forbids. Declaring the seam here lets those callers depend on
/// an abstraction while every implementation is free to ship elsewhere.
/// <para>
/// It extends <see cref="IGenericConnection"/> so a caller that needs both the connection lifecycle
/// and raw ADO takes ONE parameter rather than a connection plus a cast.
/// </para>
/// <para>
/// The return type is <see cref="DbConnection"/> so this package takes no driver dependency. A
/// caller needing provider-specific ADO casts to its own provider type — a BCL cast, not a
/// reference to the connection's package.
/// </para>
/// <para>The returned connection is UNOPENED and the caller owns it: open it and dispose it.</para>
/// </remarks>
public interface IPooledConnectionSource : IGenericConnection
{
    /// <summary>
    /// Creates a new unopened provider connection. ADO.NET pools by connection string, so repeated
    /// calls are cheap.
    /// </summary>
    /// <returns>An unopened <see cref="DbConnection"/> the caller must open and dispose.</returns>
    DbConnection CreatePooledConnection();
}

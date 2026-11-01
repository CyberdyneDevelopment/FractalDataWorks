using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// Factory contract for <see cref="MockConnectionConfiguration"/>.
/// </summary>
/// <remarks>
/// Why this exists when nothing opens a connection through it: ConnectionTypeBase is generic over its
/// factory, so an option cannot be declared without one. The tests that use this double resolve the
/// option to read its <c>ConfigurationType</c> during schema deserialization and never construct a
/// connection, so the factory is a shape the type system requires rather than a capability in use.
/// </remarks>
public interface IMockConnectionFactory : IConnectionFactory<IGenericConnection, MockConnectionConfiguration>
{
}

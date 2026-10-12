using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql.Tests.Fakes;

/// <summary>
/// Minimal IDatabasePath record for shared-base tests.
/// Carries an arbitrary dialect so tests can supply schema-aware or schemaless variants.
/// </summary>
internal sealed record FakeDatabasePath(
    string? Database,
    string? Schema,
    string ObjectName,
    ISqlDialect Dialect) : IDatabasePath;

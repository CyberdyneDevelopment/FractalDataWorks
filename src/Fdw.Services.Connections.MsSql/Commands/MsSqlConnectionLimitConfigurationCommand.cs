using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.MsSql.Limits;

namespace Fdw.Services.Connections.MsSql.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a SQL Server connection's limits, the <c>Limits</c> child of
/// <see cref="MsSqlConnectionConfiguration"/>. Its container is the one the child load reads, so
/// without it the connection read fails with NoChildCommandForType.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "MsSqlConnectionLimit")]
public sealed class MsSqlConnectionLimitConfigurationCommand : ConfigurationCommandBase<MsSqlConnectionLimitConfiguration>
{
    /// <inheritdoc/>
    public MsSqlConnectionLimitConfigurationCommand() : base("MsSqlConnectionLimit") { }
}

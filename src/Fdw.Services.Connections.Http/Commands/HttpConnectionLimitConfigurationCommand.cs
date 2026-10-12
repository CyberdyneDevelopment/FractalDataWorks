using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Http.Limits;

namespace Fdw.Services.Connections.Http.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for an HTTP connection's limits, the <c>Limits</c> child of
/// <see cref="HttpConnectionConfiguration"/>. Its container is the one the child load reads, so
/// without it the connection read fails with NoChildCommandForType.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "HttpConnectionLimit")]
public sealed class HttpConnectionLimitConfigurationCommand : ConfigurationCommandBase<HttpConnectionLimitConfiguration>
{
    /// <inheritdoc/>
    public HttpConnectionLimitConfigurationCommand() : base("HttpConnectionLimit") { }
}

using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Http.Commands;

/// <summary>ConfigurationCommands TypeOption for the HttpConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "HttpConnection")]
public sealed class HttpConnectionConfigurationCommand : ConfigurationCommandBase<HttpConnectionConfiguration>
{
    /// <inheritdoc/>
    public HttpConnectionConfigurationCommand() : base("HttpConnection") { }
}

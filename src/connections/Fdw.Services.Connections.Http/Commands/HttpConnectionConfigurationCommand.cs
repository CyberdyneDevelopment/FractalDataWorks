using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Http.Commands;

/// <summary>ConfigurationCommands TypeOption for the HttpConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "HttpConnectionConfiguration")]
public sealed class HttpConnectionConfigurationCommand : ConfigurationCommandBase<HttpConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HttpConnectionConfigurationCommand"/> class.</summary>
    public HttpConnectionConfigurationCommand() : base("HttpConnectionConfiguration") { }
}

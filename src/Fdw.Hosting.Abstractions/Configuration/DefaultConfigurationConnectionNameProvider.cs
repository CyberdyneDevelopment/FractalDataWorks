using Microsoft.Extensions.Options;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Default implementation of <see cref="IConfigurationConnectionNameProvider"/> that reads
/// the connection name from <see cref="ConfigurationConnectionOptions"/> via IOptionsMonitor.
/// </summary>
public sealed class DefaultConfigurationConnectionNameProvider : IConfigurationConnectionNameProvider
{
    private readonly IOptionsMonitor<ConfigurationConnectionOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConfigurationConnectionNameProvider"/> class.
    /// </summary>
    /// <param name="options">The configuration connection options monitor.</param>
    public DefaultConfigurationConnectionNameProvider(IOptionsMonitor<ConfigurationConnectionOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string ConnectionName => _options.CurrentValue.ConnectionName ?? "ConfigurationDb";
}

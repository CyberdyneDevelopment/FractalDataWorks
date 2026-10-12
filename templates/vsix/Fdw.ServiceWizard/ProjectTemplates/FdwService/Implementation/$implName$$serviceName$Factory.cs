using System;
using Microsoft.Extensions.Logging;
using $namespace$.$serviceName$.Abstractions;

namespace $namespace$.$serviceName$.$implName$;

/// <summary>
/// Factory for creating <see cref="$implName$$serviceName$Service"/> instances.
/// </summary>
public sealed class $implName$$serviceName$Factory : I$serviceName$Factory
{
    private readonly ILoggerFactory _loggerFactory;

    public $implName$$serviceName$Factory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public I$serviceName$Service Create(I$serviceName$Configuration configuration)
    {
        if (configuration is not $implName$$serviceName$Configuration implConfig)
        {
            throw new ArgumentException(
                $"Configuration must be of type {nameof($implName$$serviceName$Configuration)}, " +
                $"but received {configuration.GetType().Name}",
                nameof(configuration));
        }

        return new $implName$$serviceName$Service(
            _loggerFactory.CreateLogger<$implName$$serviceName$Service>(),
            implConfig);
    }
}

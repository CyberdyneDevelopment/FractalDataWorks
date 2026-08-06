using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using $namespace$.$serviceName$.Abstractions;

namespace $namespace$.$serviceName$.$implName$;

/// <summary>
/// $implName$ implementation of <see cref="I$serviceName$Service"/>.
/// </summary>
public sealed class $implName$$serviceName$Service : I$serviceName$Service
{
    private readonly ILogger<$implName$$serviceName$Service> _logger;
    private readonly $implName$$serviceName$Configuration _configuration;

    public $implName$$serviceName$Service(
        ILogger<$implName$$serviceName$Service> logger,
        $implName$$serviceName$Configuration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    // TODO: Implement I$serviceName$Service methods
}

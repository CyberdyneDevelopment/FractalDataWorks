using Fdw.Results;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>Refuses a query over a configuration resource, naming the resource.</summary>
internal sealed class ResourceQueryRefusals : IQueryRefusals
{
    private readonly string _resource;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ResourceQueryRefusals"/> class.</summary>
    /// <param name="resource">The resource queried.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    public ResourceQueryRefusals(string resource, ILogger logger)
    {
        _resource = resource;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IGenericResult<T> Invalid<T>(string reason)
        => GenericResult<T>.Failure(EndpointLogger.QueryRefused(_logger, _resource, reason));
}

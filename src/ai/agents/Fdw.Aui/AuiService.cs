using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Aui.Models;
using Fdw.Aui.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Aui;

/// <summary>
/// Service for aggregating and managing Agent User Interface (AUI) metadata.
/// </summary>
public sealed class AuiService
{
    private readonly IEnumerable<IAuiProvider> _providers;
    private readonly ILogger<AuiService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuiService"/> class.
    /// </summary>
    public AuiService(
        IEnumerable<IAuiProvider> providers,
        ILogger<AuiService> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Aggregates AUI manifests from all providers for the specified route.
    /// </summary>
    public async Task<IGenericResult<AuiManifest>> GetManifest(Guid userId, string route, CancellationToken ct = default)
    {
        try
        {
            AuiLog.ManifestRequested(_logger, route, userId);

            var aggregateManifest = new AuiManifest
            {
                Route = route,
                Description = $"Agent Interface for {route}"
            };

            // Use lists for merging as AuiManifest properties are IReadOnly
            var tools = new List<AuiTool>();
            var resources = new List<AuiResource>();
            var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in _providers)
            {
                var result = await provider.GetAuiManifest(userId, route, ct).ConfigureAwait(false);
                if (result.IsFailure)
                {
                    AuiLog.ProviderFailed(_logger, provider.GetType().Name, result.CurrentMessage ?? string.Empty);
                    continue;
                }

                if (result.Value != null)
                {
                    var source = result.Value;
                    if (!string.IsNullOrEmpty(source.Description))
                    {
                        aggregateManifest.Description = source.Description;
                    }

                    tools.AddRange(source.Tools);
                    resources.AddRange(source.Resources);

                    foreach (var kvp in source.Context)
                    {
                        context[kvp.Key] = kvp.Value;
                    }
                }
            }

            return GenericResult<AuiManifest>.Success(new AuiManifest
            {
                Route = route,
                Description = aggregateManifest.Description,
                Tools = tools.AsReadOnly(),
                Resources = resources.AsReadOnly(),
                Context = context
            });
        }
        catch (Exception ex)
        {
            return GenericResult<AuiManifest>.Failure(AuiLog.OperationFailed(_logger, ex, ex.Message));
        }
    }
}

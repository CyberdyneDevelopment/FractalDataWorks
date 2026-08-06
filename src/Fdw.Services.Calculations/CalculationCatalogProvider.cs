using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Default implementation of <see cref="ICalculationCatalogProvider"/>. Assembles the unified
/// calculation catalog by asking every registered <see cref="CalculationSourceTypes"/> option to
/// list/resolve its own entries — dispatch is always via <c>All()</c>/<c>ByName()</c>, never a
/// switch on the source name.
/// </summary>
public sealed class CalculationCatalogProvider : ICalculationCatalogProvider
{
    private readonly CalculationSourceContext _context;
    private readonly ILogger<CalculationCatalogProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="CalculationCatalogProvider"/> class.</summary>
    public CalculationCatalogProvider(ICalculationEntityService entityService, ILoggerFactory? loggerFactory)
    {
        var factory = loggerFactory ?? NullLoggerFactory.Instance;
        _context = new CalculationSourceContext(entityService, factory);
        _logger = factory.CreateLogger<CalculationCatalogProvider>();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<CalculationCatalogItem>>> Get(
        CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationCatalogLog.CatalogRequested(_logger);

            var sources = CalculationSourceTypes.All();
            var items = new List<CalculationCatalogItem>();
            foreach (var source in sources)
            {
                var listResult = await source.List(_context, cancellationToken).ConfigureAwait(false);
                if (!listResult.IsSuccess)
                {
                    // Why: a per-source failure is surfaced, not swallowed — log which source failed,
                    // then propagate the source's own result (full Code/Details/Messages) via ToNewResult
                    // rather than re-wrapping just its Messages (FDW015).
                    CalculationCatalogLog.SourceListFailed(_logger, source.Name);
                    return listResult.ToNewResult<IReadOnlyList<CalculationCatalogItem>>();
                }

                var sourceItems = listResult.Value ?? [];
                CalculationCatalogLog.SourceCatalogListed(_logger, source.Name, sourceItems.Count);
                items.AddRange(sourceItems);
            }

            CalculationCatalogLog.CatalogAssembled(_logger, items.Count, sources.Count);
            return GenericResult<IReadOnlyList<CalculationCatalogItem>>.Success(items);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<CalculationCatalogItem>>.Failure(
                CalculationCatalogLog.CatalogAssemblyFailed(_logger, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<CalculationCatalogItem>> Get(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var source in CalculationSourceTypes.All())
            {
                var result = await source.Resolve(id, _context, cancellationToken).ConfigureAwait(false);
                if (result.IsSuccess)
                    return result;
            }

            return GenericResult<CalculationCatalogItem>.Failure(
                CalculationCatalogLog.CalculationCatalogItemNotFound(_logger, "(any source)", id.ToString()));
        }
        catch (Exception ex)
        {
            return GenericResult<CalculationCatalogItem>.Failure(
                CalculationCatalogLog.CatalogAssemblyFailed(_logger, ex));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<CalculationCatalogItem>> Get(
        string source,
        string name,
        CancellationToken cancellationToken = default)
    {
        var sourceType = CalculationSourceTypes.ByName(source);
        if (sourceType == CalculationSourceTypes.NotFound)
        {
            return Task.FromResult<IGenericResult<CalculationCatalogItem>>(
                GenericResult<CalculationCatalogItem>.Failure(
                    CalculationCatalogLog.CalculationCatalogItemNotFound(_logger, source, name)));
        }

        return sourceType.Resolve(name, _context, cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Answers where the member a record cites stands, by asking the one registered domain provider whose container the citation names.
/// </summary>
/// <remarks>
/// Each cited domain's own registration adds its provider to the set this resolves from; nothing here names a domain, so a new cited domain is one
/// registration line. A citation that carries no container (the database enforces it, or it cites no domain) is not asked of anyone.
/// </remarks>
public sealed class CitedConfigurations : ICitedConfigurations
{
    private readonly IReadOnlyList<IDomainConfigurationProvider> _providers;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="CitedConfigurations"/> class.</summary>
    /// <param name="providers">Every registered domain provider a citation can name.</param>
    /// <param name="logger">The logger; a null logger is replaced by the null logger.</param>
    public CitedConfigurations(IEnumerable<IDomainConfigurationProvider> providers, ILogger<CitedConfigurations>? logger = null)
    {
        _providers = [.. providers];
        _logger = logger ?? NullLogger<CitedConfigurations>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ICitedState>> StateOf(Citation citation, Guid id, CancellationToken cancellationToken = default)
    {
        if (citation.Container is not { } container)
            return GenericResult<ICitedState>.Success(CitedStates.Unchecked);

        var named = _providers.Where(provider => string.Equals(provider.Container, container, StringComparison.Ordinal)).ToList();
        if (named.Count == 0)
            return GenericResult<ICitedState>.Failure(
                ServicesResultCodes.ByName("CitedContainerNotRegistered"), _logger,
                ResultDetails.Create("container", container, "registered", string.Join(", ", _providers.Select(provider => provider.Container).Distinct(StringComparer.Ordinal).OrderBy(registered => registered, StringComparer.Ordinal))));

        if (named.Count > 1)
            return GenericResult<ICitedState>.Failure(
                ServicesResultCodes.ByName("CitedContainerAmbiguous"), _logger,
                ResultDetails.Create("container", container, "count", named.Count));

        if (cancellationToken.IsCancellationRequested)
            return Cancelled(container);

        try
        {
            return await named[0].StateOf(id, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled(container);
        }
    }

    // The coded answer to a cancelled token: nothing was asked or the answer was not waited for, so nothing is said about the member.
    private IGenericResult<ICitedState> Cancelled(string container) =>
        GenericResult<ICitedState>.Failure(ServicesResultCodes.ByName("CitationCheckCancelled"), _logger, ResultDetails.Create("container", container));
}

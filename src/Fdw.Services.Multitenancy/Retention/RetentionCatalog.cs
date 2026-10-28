using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Serves what an application's <see cref="IRetainedDocuments"/> offers an organization to a client, after asking whether the caller
/// may read the organization's retention (FDW-2906).
/// </summary>
public sealed class RetentionCatalog : IRetentionCatalog
{
    private readonly IRetainedDocuments _documents;
    private readonly IOrganizationScope _scope;
    private readonly ILogger<RetentionCatalog> _logger;

    /// <summary>Initializes a new instance of the <see cref="RetentionCatalog"/> class.</summary>
    /// <param name="documents">The application's documents, which say what they offer and list themselves.</param>
    /// <param name="organizations">Says whether the caller holds a right for an organization, and which tenant it is in.</param>
    /// <param name="logger">The logger.</param>
    public RetentionCatalog(
        IRetainedDocuments documents, IOrganizationScope organizations, ILogger<RetentionCatalog>? logger)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _logger = logger ?? NullLogger<RetentionCatalog>.Instance;
        _scope = organizations ?? throw new ArgumentNullException(nameof(organizations));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<RetentionChoices>> Choices(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(RetentionScope.PolicyReadRight, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<RetentionChoices>();
        }

        var offer = await _documents.Offers(tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        if (offer.IsFailure || offer.Value is not { } offered)
        {
            return offer.ToNewResult<RetentionChoices>();
        }

        var types = await _documents.DocumentTypes(tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        return types.IsFailure || types.Value is not { } found
            ? types.ToNewResult<RetentionChoices>()
            : GenericResult<RetentionChoices>.Success(new RetentionChoices(offered.StartFields, offered.EndActions, found));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<RetainedDocument>>> Documents(
        Guid organizationId, string documentType, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(RetentionScope.PolicyReadRight, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<IReadOnlyList<RetainedDocument>>();
        }

        return string.IsNullOrWhiteSpace(documentType)
            ? GenericResult<IReadOnlyList<RetainedDocument>>.Failure(RetentionLog.DocumentTypeRequired(_logger, organizationId))
            : await _documents.List(tenant.Value, organizationId, documentType, cancellationToken).ConfigureAwait(false);
    }
}

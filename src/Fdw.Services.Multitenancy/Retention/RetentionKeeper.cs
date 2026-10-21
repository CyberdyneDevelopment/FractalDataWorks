using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Runs the retention policies of an organization against the application's documents (FDW-1575).
/// </summary>
/// <remarks>
/// The keeper does not own any document: it asks <see cref="IRetainedDocuments"/> for them and for each ending. A document under a legal
/// hold is never ended, however long past its deadline; a document without the date its policy counts from has no deadline and is left.
/// </remarks>
public sealed class RetentionKeeper : IRetentionKeeper
{
    private readonly IRetentionPolicies _policies;
    private readonly ILegalHolds _holds;
    private readonly IRetainedDocuments _documents;
    private readonly ILogger<RetentionKeeper> _logger;

    /// <summary>Initializes a new instance of the <see cref="RetentionKeeper"/> class.</summary>
    /// <param name="policies">The retention policies of organizations.</param>
    /// <param name="holds">The legal holds of documents.</param>
    /// <param name="documents">The application's documents.</param>
    /// <param name="logger">The logger.</param>
    public RetentionKeeper(IRetentionPolicies policies, ILegalHolds holds, IRetainedDocuments documents, ILogger<RetentionKeeper>? logger)
    {
        _policies = policies ?? throw new ArgumentNullException(nameof(policies));
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _logger = logger ?? NullLogger<RetentionKeeper>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<RetentionRunReport>> Run(Guid tenantId, Guid organizationId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var policies = await _policies.List(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (policies.IsFailure || policies.Value is not { } found)
        {
            return policies.ToNewResult<RetentionRunReport>();
        }

        var held = await _holds.HeldDocuments(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure || held.Value is not { } heldDocuments)
        {
            return held.ToNewResult<RetentionRunReport>();
        }

        var tally = new Tally(tenantId, organizationId, heldDocuments, now);
        foreach (var policy in found)
        {
            var listed = await _documents.List(tenantId, organizationId, policy.DocumentType, cancellationToken).ConfigureAwait(false);
            if (listed.IsFailure || listed.Value is not { } documents)
            {
                return GenericResult<RetentionRunReport>.Failure(RetentionLog.DocumentsNotListed(_logger, policy.DocumentType, organizationId, listed.CurrentMessage));
            }

            foreach (var document in documents)
            {
                await Judge(policy, document, tally, cancellationToken).ConfigureAwait(false);
            }
        }

        RetentionLog.RunFinished(_logger, organizationId, tally.Considered, tally.Ended.Count, tally.Held, tally.Undated, tally.Failed.Count);
        return GenericResult<RetentionRunReport>.Success(new RetentionRunReport(now, tally.Considered, tally.Held, tally.Undated, tally.Ended, tally.Failed));
    }

    // One document against its policy: left alone while undated, within its period or held; otherwise ended, or reported as failed.
    private async Task Judge(RetentionPolicy policy, RetainedDocument document, Tally tally, CancellationToken cancellationToken)
    {
        tally.Considered++;
        var deadline = RetentionClock.DeadlineFor(policy, document.Dates);
        if (deadline.KeepUntil is not { } keepUntil)
        {
            tally.Undated++;
            return;
        }

        if (keepUntil > tally.Now)
        {
            return;
        }

        if (tally.HeldDocuments.Contains(document.DocumentId))
        {
            tally.Held++;
            return;
        }

        var ended = await _documents.End(tally.TenantId, tally.OrganizationId, document, policy.EndAction, cancellationToken).ConfigureAwait(false);
        if (ended.IsFailure)
        {
            RetentionLog.DocumentNotEnded(_logger, document.DocumentId, tally.OrganizationId, policy.EndAction, ended.CurrentMessage);
            tally.Failed.Add(new RetentionOutcome(document.DocumentId, document.DocumentType, policy.EndAction, ended.CurrentMessage));
            return;
        }

        RetentionLog.DocumentEnded(_logger, document.DocumentId, document.DocumentType, tally.OrganizationId, policy.EndAction);
        tally.Ended.Add(new RetentionOutcome(document.DocumentId, document.DocumentType, policy.EndAction, null));
    }

    // What one run knows and counts: the organization it runs for, the moment it judges by, the documents held, and the totals so far.
    private sealed class Tally
    {
        internal Tally(Guid tenantId, Guid organizationId, ISet<string> heldDocuments, DateTimeOffset now)
        {
            TenantId = tenantId;
            OrganizationId = organizationId;
            HeldDocuments = heldDocuments;
            Now = now;
        }

        internal Guid TenantId { get; }

        internal Guid OrganizationId { get; }

        internal ISet<string> HeldDocuments { get; }

        internal DateTimeOffset Now { get; }

        internal int Considered { get; set; }

        internal int Held { get; set; }

        internal int Undated { get; set; }

        internal List<RetentionOutcome> Ended { get; } = [];

        internal List<RetentionOutcome> Failed { get; } = [];
    }
}

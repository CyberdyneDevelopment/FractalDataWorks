using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary>
/// <see cref="IDocumentLinks"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673). A link is
/// checked by FDW's <see cref="DocumentLinkRule"/> before it is sent, against the link kinds in force and the connection's own targets.
/// </summary>
internal sealed class ConnectionDocumentLinks : IDocumentLinks
{
    private readonly DocumentRecordCommandRunner _runner;
    private readonly IDocumentDefinitions<ILinkKindDefinition> _linkKinds;
    private readonly ILinkTargets _targets;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDocumentLinks"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    /// <param name="linkKinds">Reads the link kind definitions that bear on the organization.</param>
    /// <param name="targets">Answers whether a link's target is there.</param>
    public ConnectionDocumentLinks(DocumentRecordCommandRunner runner, IDocumentDefinitions<ILinkKindDefinition> linkKinds, ILinkTargets targets)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _linkKinds = linkKinds ?? throw new ArgumentNullException(nameof(linkKinds));
        _targets = targets ?? throw new ArgumentNullException(nameof(targets));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IDocumentLink>>> From(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListLinksFromCommand(organizationId, documentId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IDocumentLink>>> To(Guid organizationId, string targetKind, Guid targetId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListLinksToCommand(organizationId, targetKind, targetId), cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<long>> Add(Guid organizationId, IDocumentLink link, long expectedRevision, CancellationToken cancellationToken = default)
    {
        if (link is null)
        {
            throw new ArgumentNullException(nameof(link));
        }

        var linkKinds = await _linkKinds.Rows(_runner.TenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (linkKinds.IsFailure || linkKinds.Value is not { } rows)
        {
            return linkKinds.ToNewResult<long>();
        }

        var checkedLink = await DocumentLinkRule.Check(link, _runner.TenantId, rows, _targets, cancellationToken).ConfigureAwait(false);
        return checkedLink.IsFailure
            ? checkedLink.ToNewResult<long>()
            : await _runner.Run(organizationId, new AddDocumentLinkCommand(organizationId, link, expectedRevision), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IGenericResult<long>> Remove(Guid organizationId, Guid id, long expectedRevision, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new RemoveDocumentLinkCommand(organizationId, id, expectedRevision), cancellationToken);
}

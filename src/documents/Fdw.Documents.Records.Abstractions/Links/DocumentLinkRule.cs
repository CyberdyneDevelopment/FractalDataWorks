using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Links;

/// <summary>
/// FDW's check of a link before it is stored (FDW-3397): its id is a UUIDv7, its link kind is in force for the organization, its target
/// kind is one the application checks, and its target is there. The first refusal is returned and nothing is stored.
/// </summary>
public static class DocumentLinkRule
{
    /// <summary>Checks a link.</summary>
    /// <param name="link">The link.</param>
    /// <param name="tenantId">The tenant of the link's organization, whose link kind definitions bear on it.</param>
    /// <param name="linkKinds">The link kind definitions <see cref="IDocumentDefinitions{T}.Rows"/> returned for the organization.</param>
    /// <param name="targets">The application's check of targets.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>
    /// Success, or the first refusal: <c>PublicIdInvalid</c>, <c>DocumentDefinitionNotConfigured</c> for an unknown link kind,
    /// <c>DocumentLinkTargetKindUnknown</c>, <c>DocumentLinkTargetNotFound</c>, or the failure the application's check returned.
    /// </returns>
    public static async Task<IGenericResult> Check(
        IDocumentLink link, Guid tenantId, IReadOnlyList<ILinkKindDefinition> linkKinds, ILinkTargets targets, CancellationToken cancellationToken = default)
    {
        var id = PublicIds.Check(link.Id, "DocumentLink");
        if (id.IsFailure)
        {
            return id;
        }

        var kind = ScopedDefinitions.Effective(linkKinds, tenantId, link.OrganizationId, link.LinkKind);
        if (kind.IsFailure)
        {
            return kind;
        }

        var kinds = await targets.Kinds(link.OrganizationId, cancellationToken).ConfigureAwait(false);
        if (kinds.IsFailure)
        {
            return kinds;
        }

        // An answer with no list names no kind, so the target kind is unknown; it is never read as "any kind".
        if (kinds.Value is not { } named)
        {
            return TargetKindUnknown(link.TargetKind, "none: the application's check returned no list");
        }

        if (!named.Contains(link.TargetKind, StringComparer.OrdinalIgnoreCase))
        {
            return TargetKindUnknown(link.TargetKind, string.Join(", ", named));
        }

        var exists = await targets.Exists(link.OrganizationId, link.TargetKind, link.TargetId, cancellationToken).ConfigureAwait(false);
        if (exists.IsFailure)
        {
            return exists;
        }

        return exists.Value
            ? GenericResult.Success()
            : GenericResult.Failure(
                DocumentRecordResultCodes.ByName("DocumentLinkTargetNotFound"),
                ResultDetails.Create(
                    "TargetKind", link.TargetKind,
                    "TargetId", link.TargetId.ToString("D", CultureInfo.InvariantCulture),
                    "OrganizationId", link.OrganizationId.ToString("D", CultureInfo.InvariantCulture)));
    }

    private static IGenericResult TargetKindUnknown(string targetKind, string kinds) =>
        GenericResult.Failure(
            DocumentRecordResultCodes.ByName("DocumentLinkTargetKindUnknown"),
            ResultDetails.Create("TargetKind", targetKind, "Kinds", kinds));
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Lists the versions of a glossary term at the places the caller may read, nearest the caller first (FDW-2331). The places are those at or above the
/// place the caller acts in, read in one query; the term itself is the tenant's version, and the nearest version to the caller is the one that applies.
/// A place the caller may not read is absent from the answer, not blanked.
/// </summary>
internal static class GlossaryTermVersionList
{
    internal static async Task<IGenericResult<GlossaryTermVersionsResponse>> Run(
        GlossaryTermVersionServices services, IPlaceNames names, Guid termId, CallerPlace origin, string readRight, ILogger logger, CancellationToken cancellationToken)
    {
        var term = await services.Terms.Get(termId, cancellationToken).ConfigureAwait(false);
        if (term.IsFailure)
        {
            return term.ToNewResult<GlossaryTermVersionsResponse>();
        }

        if (term.Value is not { } baseTerm)
        {
            return GenericResult<GlossaryTermVersionsResponse>.Failure(GlossaryTermVersionLog.TermNotFound(logger, termId.ToString("D")));
        }

        var versions = await PlaceVersions.Entries(
            GlossaryTermVersionSource.Of(services.Versions),
            services.Ancestry,
            services.Authority,
            names,
            baseTerm.Id,
            baseTerm.Name,
            (baseTerm.Definition, baseTerm.Owner),
            row => (row.Definition, row.Owner),
            origin,
            readRight,
            logger,
            cancellationToken).ConfigureAwait(false);
        if (versions.IsFailure || versions.Value is not { } found)
        {
            return versions.ToNewResult<GlossaryTermVersionsResponse>();
        }

        var entries = found
            .Select(version => new GlossaryTermVersionResponse(
                version.PlaceKind, version.PlaceId, version.Depth, version.Payload.Definition, version.Payload.Owner, version.Label, version.IsBase, version.Applies))
            .ToList();
        GlossaryTermVersionLog.Listed(logger, baseTerm.Name, entries.Count, $"{origin.Kind} '{origin.Id:D}'");
        return GenericResult<GlossaryTermVersionsResponse>.Success(new GlossaryTermVersionsResponse(baseTerm.Id, baseTerm.Name, origin.Kind, origin.Id, entries));
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Lists the versions of a calculation at the places the caller may read, nearest the caller first (FDW-2560). The calculation itself is the tenant's
/// version, and the nearest version to the caller is the one that applies. A place the caller may not read is absent from the answer, not blanked.
/// </summary>
internal static class CalculationVersionList
{
    internal static async Task<IGenericResult<CalculationVersionsResponse>> Run(
        CalculationVersionServices services, IPlaceNames names, Guid calculationId, CallerPlace origin, string readRight, ILogger logger, CancellationToken cancellationToken)
    {
        var calculation = await services.Calculations.Get(calculationId, cancellationToken).ConfigureAwait(false);
        if (calculation.IsFailure)
        {
            return calculation.ToNewResult<CalculationVersionsResponse>();
        }

        if (calculation.Value is not { } baseCalculation)
        {
            return GenericResult<CalculationVersionsResponse>.Failure(CalculationVersionLog.CalculationNotFound(logger, calculationId.ToString("D")));
        }

        var versions = await PlaceVersions.Entries(
            CalculationVersionSource.Of(services.Versions),
            services.Ancestry,
            services.Authority,
            names,
            baseCalculation.Id,
            baseCalculation.Name,
            baseCalculation.Description,
            row => row.Description,
            origin,
            readRight,
            logger,
            cancellationToken).ConfigureAwait(false);
        if (versions.IsFailure || versions.Value is not { } found)
        {
            return versions.ToNewResult<CalculationVersionsResponse>();
        }

        var entries = found
            .Select(version => new CalculationVersionResponse(version.PlaceKind, version.PlaceId, version.Depth, version.Payload, version.Label, version.IsBase, version.Applies))
            .ToList();
        CalculationVersionLog.Listed(logger, baseCalculation.Name, entries.Count, $"{origin.Kind} '{origin.Id:D}'");
        return GenericResult<CalculationVersionsResponse>.Success(
            new CalculationVersionsResponse(baseCalculation.Id, baseCalculation.Name, origin.Kind, origin.Id, entries));
    }
}

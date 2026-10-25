using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Saves and deletes the version of a calculation held at a place (FDW-2560). The place must be an organization or a workspace of the caller's tenant,
/// and the caller must hold the write right there; the tenant's version is the calculation itself, written by the calculation's own routes.
/// </summary>
internal static class CalculationVersionWrite
{
    private const int MaxDescription = 2000;

    internal static async Task<IGenericResult<SavedCalculationVersionResponse>> Save(
        CalculationVersionServices services, SaveCalculationVersionRequest request, Guid callerTenantId, string writeRight, ILogger logger, CancellationToken cancellationToken)
    {
        var calculation = await services.Calculations.Get(request.Id, cancellationToken).ConfigureAwait(false);
        if (calculation.IsFailure)
        {
            return calculation.ToNewResult<SavedCalculationVersionResponse>();
        }

        if (calculation.Value is not { } baseCalculation)
        {
            return GenericResult<SavedCalculationVersionResponse>.Failure(CalculationVersionLog.CalculationNotFound(logger, request.Id.ToString("D")));
        }

        if (Invalid(request.Description) is { } reason)
        {
            return GenericResult<SavedCalculationVersionResponse>.Failure(CalculationVersionLog.VersionInvalid(logger, baseCalculation.Name, reason));
        }

        var allowed = await CallerPlaces.CanWrite(
            services.Ancestry, services.Authority, request.PlaceKind, request.PlaceId, callerTenantId, writeRight, logger, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.ToNewResult<SavedCalculationVersionResponse>();
        }

        var name = NameOf(baseCalculation.Name, request);
        var existing = await services.Versions.Get(name, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return existing.ToNewResult<SavedCalculationVersionResponse>();
        }

        ICalculationVersionImplementationConfiguration version;
        if (existing.Value is { } held)
        {
            version = held;
        }
        else
        {
            version = new CalculationVersionImplementationConfiguration
            {
                Name = name,
                BaseCalculationId = baseCalculation.Id,
                CalculationName = baseCalculation.Name,
                ScopeKind = request.PlaceKind,
                ScopeId = request.PlaceId,
            };
        }

        version.Description = request.Description;

        var saved = await services.Versions.Save(version, "CalculationVersion", "CalculationVersion", name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<SavedCalculationVersionResponse>();
        }

        CalculationVersionLog.Saved(logger, baseCalculation.Name, PlaceText(request));
        return GenericResult<SavedCalculationVersionResponse>.Success(new SavedCalculationVersionResponse(request.PlaceKind, request.PlaceId, request.Description));
    }

    internal static async Task<IGenericResult> Delete(
        CalculationVersionServices services, CalculationVersionPlaceRequest request, Guid callerTenantId, string writeRight, ILogger logger, CancellationToken cancellationToken)
    {
        var calculation = await services.Calculations.Get(request.Id, cancellationToken).ConfigureAwait(false);
        if (calculation.IsFailure)
        {
            return calculation;
        }

        if (calculation.Value is not { } baseCalculation)
        {
            return GenericResult.Failure(CalculationVersionLog.CalculationNotFound(logger, request.Id.ToString("D")));
        }

        var allowed = await CallerPlaces.CanWrite(
            services.Ancestry, services.Authority, request.PlaceKind, request.PlaceId, callerTenantId, writeRight, logger, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        var name = NameOf(baseCalculation.Name, request);
        var existing = await services.Versions.Get(name, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return existing;
        }

        if (existing.Value is null)
        {
            return GenericResult.Failure(CalculationVersionLog.VersionNotFound(logger, baseCalculation.Name, PlaceText(request)));
        }

        var deleted = await services.Versions.Delete(name, cancellationToken).ConfigureAwait(false);
        if (deleted.IsSuccess)
        {
            CalculationVersionLog.Deleted(logger, baseCalculation.Name, PlaceText(request));
        }

        return deleted;
    }

    // The stored name: the calculation's name joined to the place, so it is unique per place and a name still finds exactly one row.
    private static string NameOf(string calculationName, CalculationVersionPlaceRequest place)
        => string.Create(CultureInfo.InvariantCulture, $"{calculationName}@{place.PlaceKind}:{place.PlaceId:D}");

    private static string PlaceText(CalculationVersionPlaceRequest place)
        => string.Create(CultureInfo.InvariantCulture, $"{place.PlaceKind} '{place.PlaceId:D}'");

    private static string? Invalid(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "the description is empty";
        }

        return description.Length > MaxDescription
            ? $"the description is {description.Length} characters; at most {MaxDescription} are kept"
            : null;
    }
}

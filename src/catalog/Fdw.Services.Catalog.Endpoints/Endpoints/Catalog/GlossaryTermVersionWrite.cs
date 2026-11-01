using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Saves and deletes the version of a glossary term held at a place (FDW-2331). The place must be an organization or a workspace of the caller's tenant,
/// and the caller must hold the write right there; the tenant's version is the term itself, written by the term's own routes.
/// </summary>
internal static class GlossaryTermVersionWrite
{
    private const int MaxDefinition = 2000;
    private const int MaxFormula = 1000;
    private const int MaxName = 256;

    internal static async Task<IGenericResult<SavedGlossaryTermVersionResponse>> Save(
        GlossaryTermVersionServices services, SaveGlossaryTermVersionRequest request, Guid callerTenantId, string writeRight, ILogger logger, CancellationToken cancellationToken)
    {
        var term = await services.Terms.Get(request.Id, cancellationToken).ConfigureAwait(false);
        if (term.IsFailure)
        {
            return term.ToNewResult<SavedGlossaryTermVersionResponse>();
        }

        if (term.Value is not { } baseTerm)
        {
            return GenericResult<SavedGlossaryTermVersionResponse>.Failure(GlossaryTermVersionLog.TermNotFound(logger, request.Id.ToString("D")));
        }

        var invalid = Check(request, baseTerm.Name, logger);
        if (invalid is not null)
        {
            return GenericResult<SavedGlossaryTermVersionResponse>.Failure(invalid);
        }

        var allowed = await CallerPlaces.CanWrite(
            services.Ancestry, services.Authority, request.PlaceKind, request.PlaceId, callerTenantId, writeRight, logger, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.ToNewResult<SavedGlossaryTermVersionResponse>();
        }

        var name = NameOf(baseTerm.Name, request);
        var existing = await services.Versions.Get(name, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return existing.ToNewResult<SavedGlossaryTermVersionResponse>();
        }

        var version = Filled(existing.Value, name, baseTerm, request);

        var saved = await services.Versions.Save(version, "GlossaryTermVersion", "GlossaryTermVersion", name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<SavedGlossaryTermVersionResponse>();
        }

        GlossaryTermVersionLog.Saved(logger, baseTerm.Name, PlaceText(request));
        return GenericResult<SavedGlossaryTermVersionResponse>.Success(
            new SavedGlossaryTermVersionResponse(request.PlaceKind, request.PlaceId, request.Definition, request.Formula, request.Owner, request.Steward));
    }

    internal static async Task<IGenericResult> Delete(
        GlossaryTermVersionServices services, GlossaryTermVersionPlaceRequest request, Guid callerTenantId, string writeRight, ILogger logger, CancellationToken cancellationToken)
    {
        var term = await services.Terms.Get(request.Id, cancellationToken).ConfigureAwait(false);
        if (term.IsFailure)
        {
            return term;
        }

        if (term.Value is not { } baseTerm)
        {
            return GenericResult.Failure(GlossaryTermVersionLog.TermNotFound(logger, request.Id.ToString("D")));
        }

        var allowed = await CallerPlaces.CanWrite(
            services.Ancestry, services.Authority, request.PlaceKind, request.PlaceId, callerTenantId, writeRight, logger, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        var name = NameOf(baseTerm.Name, request);
        var existing = await services.Versions.Get(name, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return existing;
        }

        if (existing.Value is null)
        {
            return GenericResult.Failure(GlossaryTermVersionLog.VersionNotFound(logger, baseTerm.Name, PlaceText(request)));
        }

        var deleted = await services.Versions.Delete(name, cancellationToken).ConfigureAwait(false);
        if (deleted.IsSuccess)
        {
            GlossaryTermVersionLog.Deleted(logger, baseTerm.Name, PlaceText(request));
        }

        return deleted;
    }

    // The version to write: the one held at the place with the request's text on it, or a new one for the place when none is held.
    private static IGlossaryTermVersionImplementationConfiguration Filled(
        IGlossaryTermVersionImplementationConfiguration? existing,
        string name,
        IGlossaryTermImplementationConfiguration baseTerm,
        SaveGlossaryTermVersionRequest request)
    {
        IGlossaryTermVersionImplementationConfiguration version;
        if (existing is null)
        {
            version = new GlossaryTermVersionImplementationConfiguration
            {
                Name = name,
                BaseTermId = baseTerm.Id,
                TermName = baseTerm.Name,
                ScopeKind = request.PlaceKind,
                ScopeId = request.PlaceId,
            };
        }
        else
        {
            version = existing;
        }

        version.Definition = request.Definition;
        version.Formula = request.Formula;
        version.Owner = request.Owner;
        version.Steward = request.Steward;
        return version;
    }

    // The stored name: the term's name joined to the place, so it is unique per place and a name still finds exactly one row.
    private static string NameOf(string termName, GlossaryTermVersionPlaceRequest place)
        => string.Create(CultureInfo.InvariantCulture, $"{termName}@{place.PlaceKind}:{place.PlaceId:D}");

    private static string PlaceText(GlossaryTermVersionPlaceRequest place)
        => string.Create(CultureInfo.InvariantCulture, $"{place.PlaceKind} '{place.PlaceId:D}'");

    private static IGenericMessage? Check(SaveGlossaryTermVersionRequest request, string term, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(request.Definition))
        {
            return GlossaryTermVersionLog.VersionInvalid(logger, term, "the definition is empty");
        }

        if (request.Definition.Length > MaxDefinition)
        {
            return GlossaryTermVersionLog.VersionInvalid(logger, term, $"the definition is {request.Definition.Length} characters; at most {MaxDefinition} are kept");
        }

        if (string.IsNullOrWhiteSpace(request.Owner) || request.Owner.Length > MaxName)
        {
            return GlossaryTermVersionLog.VersionInvalid(logger, term, $"the owner is empty or longer than {MaxName} characters");
        }

        if (request.Formula is { Length: > MaxFormula })
        {
            return GlossaryTermVersionLog.VersionInvalid(logger, term, $"the formula is longer than {MaxFormula} characters");
        }

        return request.Steward is { Length: > MaxName }
            ? GlossaryTermVersionLog.VersionInvalid(logger, term, $"the steward is longer than {MaxName} characters")
            : null;
    }
}

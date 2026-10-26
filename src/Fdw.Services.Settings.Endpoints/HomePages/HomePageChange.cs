using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Settings.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Reads, saves and publishes the home page of a scope the caller has already been allowed to use, and records the change (FDW-2471). One flow for
/// every scope, so an organization's, a tenant's and a person's page answer and record alike.
/// </summary>
internal static class HomePageChange
{
    internal static async Task<IGenericResult<HomePageResponse>> Read(IHomePages pages, HomePageScope scope, CancellationToken cancellationToken)
    {
        var set = await pages.Get(scope, cancellationToken).ConfigureAwait(false);
        return set.IsFailure || set.Value is not { } held
            ? set.ToNewResult<HomePageResponse>()
            : GenericResult<HomePageResponse>.Success(HomePageJson.Of(held));
    }

    internal static async Task<IGenericResult<HomePageResponse>> Save(
        IHomePages pages, IAdministrativeAudit audit, ILogger logger, HomePageScope scope, JsonElement page, CancellationToken cancellationToken)
    {
        var body = page.GetRawText();
        var saved = await pages.SaveDraft(scope, body, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
            return saved.ToNewResult<HomePageResponse>();

        var description = string.Create(CultureInfo.InvariantCulture, $"Draft saved, {body.Length} characters");
        return await Recorded(pages, audit, logger, scope, HomePageAudit.DraftSaved, description, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<IGenericResult<HomePageResponse>> Publish(
        IHomePages pages, IAdministrativeAudit audit, ILogger logger, HomePageScope scope, CancellationToken cancellationToken)
    {
        var published = await pages.Publish(scope, cancellationToken).ConfigureAwait(false);
        if (published.IsFailure)
            return published.ToNewResult<HomePageResponse>();

        return await Recorded(pages, audit, logger, scope, HomePageAudit.Published, "Draft published", cancellationToken).ConfigureAwait(false);
    }

    // A change that was made but could not be recorded is answered as a failure, never as a success with the record missing.
    private static async Task<IGenericResult<HomePageResponse>> Recorded(
        IHomePages pages, IAdministrativeAudit audit, ILogger logger, HomePageScope scope, string changeKind, string description, CancellationToken cancellationToken)
    {
        var audited = await HomePageAudit.Record(audit, changeKind, scope, description, cancellationToken).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            return GenericResult<HomePageResponse>.Failure(
                HomePageEndpointLog.ChangeNotAudited(logger, changeKind, HomePageAudit.ItemOf(scope), audited.CurrentMessage));
        }

        return await Read(pages, scope, cancellationToken).ConfigureAwait(false);
    }
}

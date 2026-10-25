using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// The audit entries for home pages saved and published (FDW-2471). A tenant's or an organization's page is shared, so a change to it is an
/// administrative change and is recorded at that level. A person's own page is theirs alone: it has no administrative level and is not recorded.
/// </summary>
internal static class HomePageAudit
{
    internal const string DraftSaved = "HomePageDraftSaved";

    internal const string Published = "HomePagePublished";

    private const string ItemKind = "HomePage";

    internal static string ItemOf(HomePageScope scope) => string.Create(CultureInfo.InvariantCulture, $"{scope.Kind}:{scope.Id:D}");

    internal static Task<IGenericResult> Record(IAdministrativeAudit audit, string changeKind, HomePageScope scope, string description, CancellationToken cancellationToken)
    {
        if (string.Equals(scope.Kind, HomePageKinds.Organization, StringComparison.Ordinal))
        {
            return audit.Record(
                new AdministrativeChangeEntry(changeKind, ItemKind, ItemOf(scope), AdministrativeLevels.ByName("Organization"), scope.Id, null, description, null),
                cancellationToken);
        }

        if (string.Equals(scope.Kind, HomePageKinds.Tenant, StringComparison.Ordinal))
        {
            return audit.Record(
                new AdministrativeChangeEntry(changeKind, ItemKind, ItemOf(scope), AdministrativeLevels.ByName("Tenant"), null, null, description, null),
                cancellationToken);
        }

        return Task.FromResult(GenericResult.Success());
    }
}

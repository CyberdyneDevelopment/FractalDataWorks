using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Settings;

/// <summary>
/// Reads, saves and publishes the home page of a scope, and says which page applies to a person (FDW-2325).
/// </summary>
/// <remarks>
/// A page is held as the JSON the console wrote, checked only for being a page (a version and cards); what a card is, where its data comes
/// from and who may change a scope's page are not decided here.
/// </remarks>
public interface IHomePages
{
    /// <summary>Reads the published page and the draft of a scope.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Both, each null when the scope holds none.</returns>
    Task<IGenericResult<HomePageSet>> Get(HomePageScope scope, CancellationToken cancellationToken = default);

    /// <summary>Saves the draft of a scope, apart from its published page.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="body">The page as JSON.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success; a failure when the body is not a page.</returns>
    Task<IGenericResult> SaveDraft(HomePageScope scope, string body, CancellationToken cancellationToken = default);

    /// <summary>Makes the draft of a scope its published page.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success; a conflict when the scope holds no draft.</returns>
    Task<IGenericResult> Publish(HomePageScope scope, CancellationToken cancellationToken = default);

    /// <summary>Says which published page applies to a person: their own, else their organization's, else their tenant's, else none.</summary>
    /// <param name="userId">The person.</param>
    /// <param name="tenantId">The tenant they act in.</param>
    /// <param name="organizationId">The organization they act in; null when they act in none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page and the scope it comes from, or none.</returns>
    Task<IGenericResult<AppliedHomePage>> Applying(Guid userId, Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default);
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Aui.Models;

namespace Fdw.Aui;

/// <summary>
/// Defines a provider that contributes semantic metadata to the Agent User Interface (AUI).
/// </summary>
public interface IAuiProvider
{
    /// <summary>
    /// Gets the AUI manifest for the specified route and user context.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="route">The current navigation route.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the AUI manifest.</returns>
    Task<IGenericResult<AuiManifest>> GetAuiManifest(Guid userId, string route, CancellationToken ct = default);
}

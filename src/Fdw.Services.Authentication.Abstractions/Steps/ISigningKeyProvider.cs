using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Supplies an external authority's current signing keys.
/// </summary>
/// <remarks>
/// Keys rotate. An implementation must be able to refresh when a signature fails against everything
/// it holds, or a rotation blinds verification until the next scheduled fetch — twelve hours, on
/// Microsoft's defaults.
/// </remarks>
public interface ISigningKeyProvider
{
    /// <summary>Returns the keys published at <paramref name="jwksUri"/>.</summary>
    /// <param name="jwksUri">Where the authority publishes its keys.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<IReadOnlyCollection<SecurityKey>>> Current(
        Uri jwksUri, CancellationToken cancellationToken = default);
}

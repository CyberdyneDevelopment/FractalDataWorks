using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// The <see cref="ITokenIssuer"/> a host resolves from its own configuration.
/// </summary>
/// <remarks>
/// A pass-through so the container can hand out <see cref="ITokenIssuer"/> during registration,
/// while what actually mints is built on first use from rows that cannot be read until the
/// container exists. <see cref="Issue"/> is already asynchronous, so the read costs nothing extra —
/// nothing blocks, and the failure to resolve is returned as the result of the call that needed it.
/// </remarks>
internal sealed class ConfiguredTokenIssuer : ITokenIssuer
{
    private readonly JwtIssuanceResolver _resolver;

    /// <summary>Initializes a new instance of the <see cref="ConfiguredTokenIssuer"/> class.</summary>
    /// <param name="resolver">Resolves the configured issuer.</param>
    public ConfiguredTokenIssuer(JwtIssuanceResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IssuedToken>> Issue(
        IssuanceRequest request, CancellationToken cancellationToken = default)
    {
        var issuer = await _resolver.Issuer(cancellationToken).ConfigureAwait(false);

        return issuer.IsFailure || issuer.Value is not { } resolved
            ? issuer.ToNewResult<IssuedToken>()
            : await resolved.Issue(request, cancellationToken).ConfigureAwait(false);
    }
}

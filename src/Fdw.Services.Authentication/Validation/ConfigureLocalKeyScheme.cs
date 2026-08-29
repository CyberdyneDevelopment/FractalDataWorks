using System;
using System.Threading;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Supplies one <see cref="LocalKeyAuthenticationType"/> scheme with the key it validates against.
/// </summary>
/// <remarks>
/// Separate from the option because of when it runs. An option registers during startup, before a
/// secret manager exists to be asked; this runs the first time the scheme is used, inside the built
/// container, which is the first moment the key can actually be fetched.
/// </remarks>
internal sealed class ConfigureLocalKeyScheme : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly string _schemeName;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly IServiceProvider _services;

    /// <summary>Initializes a new instance of the <see cref="ConfigureLocalKeyScheme"/> class.</summary>
    /// <param name="schemeName">The scheme this configures.</param>
    /// <param name="issuer">The issuer tokens must name.</param>
    /// <param name="audience">The audience tokens must name.</param>
    /// <param name="services">The container the signing credential provider comes from.</param>
    public ConfigureLocalKeyScheme(
        string schemeName,
        string issuer,
        string audience,
        IServiceProvider services)
    {
        _schemeName = schemeName ?? throw new ArgumentNullException(nameof(schemeName));
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _audience = audience ?? throw new ArgumentNullException(nameof(audience));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    /// <remarks>Named schemes only — an unnamed call configures every JwtBearer scheme in the host.</remarks>
    public void Configure(JwtBearerOptions options)
    {
    }

    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.Equals(name, _schemeName, StringComparison.Ordinal))
            return;

        var log = _services.GetService<ILoggerFactory>()?.CreateLogger<ConfigureLocalKeyScheme>()
            ?? NullLogger<ConfigureLocalKeyScheme>.Instance;

        // No Authority: setting it is what triggers metadata discovery over the network, which is
        // the whole reason this scheme exists apart from JwtBearer.
        options.Audience = _audience;
        options.MapInboundClaims = false;

        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidIssuer = _issuer;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidAudience = _audience;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.RoleClaimType = ClaimDefinitions.roles.Name;
        options.TokenValidationParameters.NameClaimType = ClaimDefinitions.sub.Name;

        // Pinned rather than read from the token's own header: an attacker who chooses the
        // algorithm can choose one this key trivially satisfies.
        options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256];

        // VSTHRD002: IConfigureNamedOptions.Configure is ASP.NET's contract and returns void, so a
        // key fetched here can only be waited on. It is safe in this one place and not in general:
        // this runs once per scheme on first use, on a thread pool thread with no synchronization
        // context to deadlock against, and the provider caches so no later request repeats it.
        //
        // The alternative was IssuerSigningKeyResolver, which would move the same blocking call
        // onto every request instead of one.
#pragma warning disable VSTHRD002
        var credentials = _services
            .GetRequiredService<ISigningCredentialProvider>()
            .Current(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
#pragma warning restore VSTHRD002

        if (credentials.IsSuccess && credentials.Value is { Key: { } key })
        {
            options.TokenValidationParameters.IssuerSigningKey = key;
            return;
        }

        // Left without a key rather than throwing. ValidateIssuerSigningKey stays on, so every
        // token fails its signature check - which is the correct answer when the key that would
        // check it cannot be read, and the log line says which of the two happened.
        AuthenticationValidationLog.LocalSigningKeyUnavailable(log, _schemeName);
    }
}

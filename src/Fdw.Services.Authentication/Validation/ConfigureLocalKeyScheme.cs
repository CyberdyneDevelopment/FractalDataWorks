using System;
using System.Linq;
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
/// Supplies a LocalKey scheme's validation parameters, read through the configuration provider.
/// </summary>
/// <remarks>
/// <c>JwtBearerHandler</c> takes its options from <c>IOptionsMonitor&lt;JwtBearerOptions&gt;.Get(scheme)</c>.
/// That is its contract and there is no way past it, so this exists as the adapter between it and the
/// configuration system — one for the whole domain rather than one per declared entry, and it reads
/// through <see cref="IAuthenticationServiceConfigurationProvider"/> when the scheme is first used
/// rather than holding values captured while the container was still being described.
/// <para>
/// Registered as <see cref="IConfigureOptions{TOptions}"/>, which is the service type
/// <c>OptionsFactory</c> resolves — it takes <c>IEnumerable&lt;IConfigureOptions&lt;TOptions&gt;&gt;</c>
/// and asks each entry whether it is also <see cref="IConfigureNamedOptions{TOptions}"/>. Registered
/// under the named interface instead, it sits in a collection nothing reads, and the scheme then
/// validates with no key and no issuer.
/// </para>
/// </remarks>
internal sealed class ConfigureLocalKeyScheme : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IServiceProvider _services;

    /// <summary>Initializes a new instance of the <see cref="ConfigureLocalKeyScheme"/> class.</summary>
    /// <param name="services">The built container the configuration and the signing key come from.</param>
    public ConfigureLocalKeyScheme(IServiceProvider services)
        => _services = services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
    /// <remarks>Named schemes only — an unnamed call would configure every JwtBearer scheme in the host.</remarks>
    public void Configure(JwtBearerOptions options)
    {
    }

    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (name is null || !name.StartsWith(LocalKeyAuthenticationType.SchemePrefix, StringComparison.Ordinal))
            return;

        var log = _services.GetService<ILoggerFactory>()?.CreateLogger<ConfigureLocalKeyScheme>()
            ?? NullLogger<ConfigureLocalKeyScheme>.Instance;

        var serviceName = name[LocalKeyAuthenticationType.SchemePrefix.Length..];
        var provider = _services.GetRequiredService<IAuthenticationServiceConfigurationProvider>();

        // VSTHRD002: IConfigureNamedOptions.Configure is ASP.NET's contract and returns void, so an
        // asynchronous read here can only be waited on. Safe in this one place: it runs once per
        // scheme on first use, on a thread pool thread with no synchronization context, and both the
        // provider and the gateway cache, so no later request repeats it.
#pragma warning disable VSTHRD002
        var headers = provider.GetHeaders(CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        // The issuer is on the domain row: every kind has one, and it is what routed the token here.
        var header = headers.IsSuccess && headers.Value is not null
            ? headers.Value.FirstOrDefault(e =>
                string.Equals(e.Name, serviceName, StringComparison.OrdinalIgnoreCase))
            : null;

        if (header is null)
        {
            // Left unconfigured rather than throwing. ValidateIssuerSigningKey stays on and no key is
            // set, so every token fails its signature check — the correct answer when the entry that
            // says how to check it cannot be read, and the log line says which of the two happened.
            AuthenticationValidationLog.LocalKeyEntryUnreadable(log, serviceName);
            return;
        }

        // The audience is on the implementation row, which the provider dispatches to by the kind the
        // domain row names.
#pragma warning disable VSTHRD002
        var implementation = provider.Get(header.Id, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        if (!implementation.IsSuccess || implementation.Value is not ILocalKeyAuthenticationConfiguration body)
        {
            AuthenticationValidationLog.LocalKeyEntryUnreadable(log, serviceName);
            return;
        }

        // No Authority: setting it is what triggers metadata discovery over the network, which is the
        // whole reason this option exists apart from JwtBearer.
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidateIssuer = true;
        // Through the same rule the binding was built with — see IssuerName.
        var issuer = IssuerName.Read(header.Authority, serviceName, log);
        if (!issuer.IsSuccess || issuer.Value is null)
        {
            AuthenticationValidationLog.LocalKeyEntryUnreadable(log, serviceName);
            return;
        }

        options.TokenValidationParameters.ValidIssuer = issuer.Value;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidAudience = body.Audience;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.RoleClaimType = ClaimDefinitions.roles.Name;
        options.TokenValidationParameters.NameClaimType = ClaimDefinitions.sub.Name;

        // Pinned rather than read from the token's own header: an attacker who chooses the algorithm
        // can choose one this key trivially satisfies.
        options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256];

#pragma warning disable VSTHRD002
        var credentials = _services.GetRequiredService<ISigningCredentialProvider>()
            .Current(CancellationToken.None)
            .GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        if (credentials.IsSuccess && credentials.Value is { Key: { } key })
        {
            options.TokenValidationParameters.IssuerSigningKey = key;
            return;
        }

        AuthenticationValidationLog.LocalSigningKeyUnavailable(log, name);
    }

}

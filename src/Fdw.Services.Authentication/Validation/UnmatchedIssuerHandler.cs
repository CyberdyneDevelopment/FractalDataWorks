using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// The scheme <see cref="IssuerSchemeSelector"/> routes to when no declared authentication service
/// accepts the request's issuer. It authenticates nothing, ever.
/// </summary>
/// <remarks>
/// ASP.NET's forwarding selector must name a scheme, so "no scheme accepts this issuer" needs a scheme
/// to be expressible at all. Naming one of the real validators instead would hand a token to a
/// validator that was never declared for its issuer, and read afterwards as that validator's rejection
/// rather than as the undeclared issuer it is. This one fails with that reason, and the reason is
/// already in the log by the time it runs.
/// </remarks>
public sealed class UnmatchedIssuerHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>The scheme name for the no-declared-issuer outcome.</summary>
    public const string SchemeName = "Fdw.UnmatchedIssuer";

    /// <summary>Initializes a new instance of the <see cref="UnmatchedIssuerHandler"/> class.</summary>
    /// <param name="options">The scheme options monitor.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public UnmatchedIssuerHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.Fail(
            "No authentication service is declared for this token's issuer."));
}

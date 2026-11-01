using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Sessions.Csrf;

/// <summary>Base class for the options of <see cref="SessionCsrfProtectionTypes"/>.</summary>
public abstract class SessionCsrfProtectionBase : TypeOptionBase<SessionCsrfProtectionBase>, ISessionCsrfProtection
{
    /// <summary>Initializes a new instance of the <see cref="SessionCsrfProtectionBase"/> class.</summary>
    /// <param name="name">The name a Session service's <c>CsrfProtection</c> column carries.</param>
    protected SessionCsrfProtectionBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult ValidateConfiguration(ISessionAuthenticationConfiguration configuration, ILogger logger);

    /// <inheritdoc />
    public abstract Task<IGenericResult> Verify(
        HttpRequest request, SessionRecord session, ISessionAuthenticationConfiguration configuration, ILogger logger,
        CancellationToken cancellationToken = default);
}

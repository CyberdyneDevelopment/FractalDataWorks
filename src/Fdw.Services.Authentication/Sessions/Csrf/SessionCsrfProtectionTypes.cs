using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Sessions.Csrf;

/// <summary>
/// The CSRF protections a Session authentication service can name in its <c>CsrfProtection</c>
/// column (FDW-903).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SessionCsrfProtectionBase), typeof(ISessionCsrfProtection), typeof(SessionCsrfProtectionTypes))]
public abstract partial class SessionCsrfProtectionTypes : TypeCollectionBase<SessionCsrfProtectionBase, ISessionCsrfProtection>
{
}

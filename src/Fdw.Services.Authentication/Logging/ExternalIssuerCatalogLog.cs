using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Flow;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for reading the issuers this deployment can resolve.
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class ExternalIssuerCatalogLog
{
    /// <summary>An authority could not state the issuer it hands callers to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="step">The federating step that could not answer.</param>
    /// <param name="reason">Why it could not.</param>
    /// <remarks>
    /// Warning rather than Error because the set is still usable: a binding naming a readable
    /// issuer is still answered. What it costs is the ability to REFUSE an unrecognised one, and
    /// that refusal turns into "cannot determine" while this is true.
    /// </remarks>
    [MessageLogging(EventId = 91252, Level = LogLevel.Warning,
        Message = "The federating step '{step}' could not state its issuer ({reason}): bindings naming an unrecognised issuer cannot be refused until it can")]
    internal static partial IGenericMessage IssuerUnreadable(
        ILogger<ExternalIssuerCatalog> logger, string step, string reason);
}

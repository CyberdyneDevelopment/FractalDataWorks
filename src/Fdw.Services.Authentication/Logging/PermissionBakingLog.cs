using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for baking a principal's permissions into a token.
/// </summary>
/// <remarks>EventId range: 91245–91246.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class PermissionBakingLog
{
    /// <summary>Permissions were resolved and staged for the token.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">Whose they are.</param>
    /// <param name="count">How many.</param>
    // The count, not the permissions: the list is long enough to bury every other line at Trace,
    // and the count is what answers the question actually asked of this log - whether baking
    // happened at all. A token carrying zero permissions is the failure worth seeing.
    [MessageLogging(EventId = 91245, Level = LogLevel.Trace,
        Message = "Baked {count} permission(s) for principal {principalId}")]
    internal static partial IGenericMessage Baked(
        ILogger<BakePermissionsStep> logger, Guid principalId, int count);
}

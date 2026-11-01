using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints.Logging;

/// <summary>MessageLogging for the organization endpoints (FDW-1016).</summary>
[MessageLoggingTypeCode("TENANTENDPOINTS")]
public static partial class OrganizationEndpointLog
{
    /// <summary>Logs the tenant whose organizations a list is reading.</summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information, Message = "Listing the organizations of tenant '{tenantId}'")]
    public static partial IGenericMessage ListingOrganizations(ILogger logger, string tenantId);
}

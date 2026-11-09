using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Logging;

/// <summary>
/// Messages of the decision which organization a new pipeline belongs to (FDW-4573). Each reason a create is refused is its own message with its own number and client text.
/// </summary>
[MessageLoggingTypeCode("PIPELINEOWNER")]
public static partial class PipelineOwnerLog
{
    /// <summary>Logs the organization chosen for a new pipeline and where it came from.</summary>
    [MessageLogging(EventId = 11800, Level = LogLevel.Debug, Message = "The new pipeline belongs to organization {organizationId}, from {source}")]
    public static partial IGenericMessage OrganizationChosen(ILogger logger, Guid organizationId, string source);

    /// <summary>Logs a create that names no organization and is made by a caller who acts in none.</summary>
    [MessageLogging(EventId = 20006, Level = LogLevel.Warning,
        Message = "A pipeline cannot be made: the request names no organization and the caller acts in none",
        ClientMessage = "A pipeline belongs to an organization: name one in 'orgId', or act in one")]
    public static partial IGenericMessage NoOrganization(ILogger logger);

    /// <summary>Logs a create naming an organization that belongs to a tenant other than the one the caller acts in.</summary>
    [MessageLogging(EventId = 20007, Level = LogLevel.Warning,
        Message = "A pipeline cannot be made in organization {organizationId}: it belongs to tenant {organizationTenantId}, and the caller acts in tenant {callerTenantId}",
        ClientMessage = "A pipeline can only be made in an organization of the tenant you act in")]
    public static partial IGenericMessage OrganizationOfAnotherTenant(ILogger logger, Guid organizationId, Guid organizationTenantId, Guid? callerTenantId);

    /// <summary>Logs a create naming an organization that is not active.</summary>
    [MessageLogging(EventId = 20008, Level = LogLevel.Warning,
        Message = "A pipeline cannot be made in organization {organizationId}: the organization is not active",
        ClientMessage = "A pipeline cannot be made in an organization that is not active")]
    public static partial IGenericMessage OrganizationNotActive(ILogger logger, Guid organizationId);

    /// <summary>Logs an update that names an organization other than the one the pipeline belongs to.</summary>
    [MessageLogging(EventId = 20009, Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' belongs to organization {storedOrganizationId}; an update naming organization {requestedOrganizationId} is refused",
        ClientMessage = "An update cannot move pipeline '{pipelineName}' to another organization")]
    public static partial IGenericMessage OrganizationCannotChange(ILogger logger, string pipelineName, Guid? storedOrganizationId, Guid requestedOrganizationId);
}

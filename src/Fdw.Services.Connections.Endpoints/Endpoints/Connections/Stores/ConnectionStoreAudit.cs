using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// The audit entries for the connections granted to tenants (Platform level) and assigned to organizations (Organization level)
/// (FDW-1581).
/// </summary>
internal static class ConnectionStoreAudit
{
    private const string GrantItemKind = "ConnectionGrant";
    private const string AssignmentItemKind = "ConnectionAssignment";

    internal static Task<IGenericResult> Grant(IAdministrativeAudit audit, string changeKind, Guid tenantId, string connectionName, string purpose,
        string? oldValue, string? newValue, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, GrantItemKind, $"{tenantId}:{purpose}:{connectionName}", AdministrativeLevels.ByName("Platform"), null,
                oldValue, newValue, null),
            cancellationToken);

    internal static Task<IGenericResult> Assignment(IAdministrativeAudit audit, string changeKind, Guid organizationId, string purpose,
        string? oldValue, string? newValue, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, AssignmentItemKind, $"{organizationId}:{purpose}", AdministrativeLevels.ByName("Organization"), organizationId,
                oldValue, newValue, null),
            cancellationToken);

    internal static string Describe(Guid grantId, string? connectionName, string? grantFolder, string? baseFolder)
        => $"Grant='{grantId}'; Connection='{connectionName}'; GrantFolder='{grantFolder}'; BaseFolder='{baseFolder}'";
}

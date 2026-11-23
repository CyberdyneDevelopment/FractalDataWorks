using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Role configuration.</summary>
/// <remarks>
/// A role is named by Id from two other domains: each person's assignment of it (UserRole) and each permission it grants (RolePermission).
/// Deleting a role removes both in the same unit of work as the role (FDW-5222), so no assignment or grant is left naming a role that no
/// longer exists, whoever deletes it.
/// </remarks>
public sealed class RoleConfigurationProvider
    : DomainConfigurationProviderBase<IRoleImplementationConfiguration, RoleDomainConfiguration>,
      IRoleConfigurationProvider,
      IReferencedConfigurationProvider<IRoleImplementationConfiguration>,
      IConfigurationWriteObserver<IRoleImplementationConfiguration>,
      IConfigurationReadObserver<IRoleImplementationConfiguration>,
      IConfigurationDomainReadObserver<RoleDomainConfiguration>
{
    private const string DescriptionType = RoleDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;
    private readonly ILogger<RoleConfigurationProvider> _logger;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly IRolePermissionConfigurationProvider _rolePermissions;

    /// <summary>Initializes a new instance of the <see cref="RoleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="userRoles">The assignments of roles to people, which a role's delete removes.</param>
    /// <param name="rolePermissions">The permissions roles grant, which a role's delete removes.</param>
    /// <param name="writer">Writes each role's description in the unit of the role's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the roles a read has just composed, once for the set or the page (FDW-6208).</param>
    public RoleConfigurationProvider(
        ILogger<RoleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IUserRoleConfigurationProvider userRoles,
        IRolePermissionConfigurationProvider rolePermissions,
        IDescriptionWriter writer,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _logger = logger ?? NullLogger<RoleConfigurationProvider>.Instance;
        _userRoles = userRoles;
        _rolePermissions = rolePermissions;
        _writer = writer;
        _reader = reader;
    }

    /// <inheritdoc />
    /// <remarks>A role saved with no text removes the one it had; nothing invents one.</remarks>
    public Task<IGenericResult> Written(IRoleImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A role with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<IRoleImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, r => r.Id, (r, text) => r.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The role lists and queries read one page of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<RoleDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, r => r.Id, (r, text) => r.Description = text, cancellationToken);

    /// <inheritdoc/>
    public async Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var assignments = await _userRoles.RemoveAssignmentsOf(memberId, unit, cancellationToken).ConfigureAwait(false);
        if (assignments.IsFailure) return assignments;

        var grants = await _rolePermissions.RemoveGrantsOf(memberId, unit, cancellationToken).ConfigureAwait(false);
        if (grants.IsFailure) return grants;

        var described = await _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken).ConfigureAwait(false);
        if (described.IsFailure) return described;

        RoleConfigurationProviderLog.RoleReferencesRemoved(_logger, memberId.ToString("D"), assignments.Value, grants.Value);
        return GenericResult.Success();
    }
}

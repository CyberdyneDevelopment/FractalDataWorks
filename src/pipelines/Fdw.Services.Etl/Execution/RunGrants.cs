using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Runs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The stored grants of runs (FDW-6144): the person's own configuration in <c>pipe.RunGrant</c> and <c>pipe.RunGrantPermission</c>, written and retired through the configuration save and retire path and read
/// through the platform's configuration gateway, in the caller's tenant (the row-level security of the tenant filters what is read). The person of a grant is the caller who issues it, read by the rule every
/// run's person is read by (<see cref="RunPerson"/>); no request can state one.
/// </summary>
public sealed class RunGrants : IRunGrants
{
    private const string PathName = "pipe";
    private const string GrantContainerName = "RunGrant";
    private const string PermissionContainerName = "RunGrantPermission";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IAuthenticationContextAccessor _authentication;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RunGrants"/> class.</summary>
    /// <param name="gateways">Gives the platform's configuration gateway.</param>
    /// <param name="authentication">Gives the caller, who is the person of a grant.</param>
    /// <param name="time">The clock a grant's end is judged against.</param>
    /// <param name="logger">The logger.</param>
    public RunGrants(IConfigurationGatewayProvider gateways, IAuthenticationContextAccessor authentication, TimeProvider time, ILogger<RunGrants>? logger = null)
    {
        _gateways = gateways;
        _authentication = authentication;
        _time = time;
        _logger = logger ?? NullLogger<RunGrants>.Instance;
    }

    private static DataStoreTarget GrantTarget => new(PlatformConnection.Name, PathName, GrantContainerName);

    private static DataStoreTarget PermissionTarget => new(PlatformConnection.Name, PathName, PermissionContainerName);

    /// <inheritdoc />
    public async Task<IGenericResult<RunGrant>> Issue(IReadOnlyCollection<string> permissions, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        if (RunPerson.Of(_authentication.Current) is not { } person)
        {
            return GenericResult<RunGrant>.Failure(RunGrantLog.PersonMissing(_logger));
        }

        var allowed = permissions.Where(permission => !string.IsNullOrWhiteSpace(permission)).Select(permission => permission.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (allowed.Count == 0)
        {
            return GenericResult<RunGrant>.Failure(RunGrantLog.PermissionsMissing(_logger, person));
        }

        if (expiresAt <= _time.GetUtcNow())
        {
            return GenericResult<RunGrant>.Failure(RunGrantLog.ExpiryNotFuture(_logger, person, expiresAt, _time.GetUtcNow()));
        }

        var row = new RunGrantRecord { PersonId = person, ExpiresAt = expiresAt };

        // Why the permissions are written first: the grant is read through its own row, so a grant whose permissions did not all get written is never found, and a permission row without a grant is never read.
        foreach (var permission in allowed)
        {
            var written = await Gateway().Execute<ConfigurationWriteResult>(
                new ConfigurationSaveCommand<RunGrantPermissionRecord>(new RunGrantPermissionRecord { GrantId = row.Id, Permission = permission }, replacesCurrent: true), PermissionTarget, cancellationToken)
                .ConfigureAwait(false);
            if (written.IsFailure)
            {
                return GenericResult<RunGrant>.Failure(RunGrantLog.NotWritten(_logger, row.Id, written.CurrentMessage));
            }
        }

        var saved = await Gateway().Execute<ConfigurationWriteResult>(new ConfigurationSaveCommand<RunGrantRecord>(row, replacesCurrent: true), GrantTarget, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? GenericResult<RunGrant>.Failure(RunGrantLog.NotWritten(_logger, row.Id, saved.CurrentMessage))
            : GenericResult<RunGrant>.Success(Of(row, allowed));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<RunGrant>> Read(Guid grantId, CancellationToken cancellationToken = default)
    {
        var rows = await Gateway().Execute<IEnumerable<RunGrantRecord>>(
            Query.From<RunGrantRecord>(PlatformConnection.Name, PathName, GrantContainerName).Where("Id", grantId).Where("IsCurrent", true).Build(), cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            return GenericResult<RunGrant>.Failure(RunGrantLog.NotRead(_logger, grantId, rows.CurrentMessage));
        }

        if (rows.Value.FirstOrDefault() is not { } row)
        {
            return GenericResult<RunGrant>.Failure(RunGrantLog.NotFound(_logger, grantId));
        }

        var permissions = await Gateway().Execute<IEnumerable<RunGrantPermissionRecord>>(
            Query.From<RunGrantPermissionRecord>(PlatformConnection.Name, PathName, PermissionContainerName).Where("GrantId", grantId).Where("IsCurrent", true).Where("IsDeleted", false).Build(), cancellationToken)
            .ConfigureAwait(false);
        return permissions.IsFailure || permissions.Value is null
            ? GenericResult<RunGrant>.Failure(RunGrantLog.NotRead(_logger, grantId, permissions.CurrentMessage))
            : GenericResult<RunGrant>.Success(Of(row, [.. permissions.Value.Select(permission => permission.Permission)]));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Revoke(Guid grantId, CancellationToken cancellationToken = default)
    {
        var found = await Read(grantId, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found;
        }

        var retired = await Gateway().Execute<ConfigurationWriteResult>(
            new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [grantId], markDeleted: true), GrantTarget, cancellationToken).ConfigureAwait(false);
        return retired.IsFailure ? GenericResult.Failure(RunGrantLog.NotWritten(_logger, grantId, retired.CurrentMessage)) : GenericResult.Success();
    }

    private IDataGateway Gateway()
    {
        var found = _gateways.Get(PlatformConnection.Name);
        return found.IsSuccess && found.Value is { } gateway
            ? gateway
            : throw new InvalidOperationException($"No configuration gateway is available for '{PlatformConnection.Name}'; it must be declared in the host's configurationSchema.json, with the tables {PathName}.{GrantContainerName} and {PathName}.{PermissionContainerName}.");
    }

    private static RunGrant Of(RunGrantRecord row, IReadOnlyList<string> permissions) =>
        new() { Id = row.Id, PersonId = row.PersonId, ExpiresAt = row.ExpiresAt, Permissions = permissions, IsRevoked = row.IsDeleted };
}

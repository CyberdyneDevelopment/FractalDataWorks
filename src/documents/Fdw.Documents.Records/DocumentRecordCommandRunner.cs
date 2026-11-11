using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Documents.Records.Logging;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Documents.Records;

/// <summary>
/// Sends the store's commands to the organization's DocumentRecord connection (FDW-3673). A command is refused before it is sent when it
/// names another organization or tenant than the store was found for, or when its own <see cref="IDocumentRecordCommand.FirstProblem"/>
/// finds something: a connection several organizations share must never be asked for another organization's records.
/// </summary>
internal sealed class DocumentRecordCommandRunner
{
    private readonly IDocumentRecordConnection _connection;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DocumentRecordCommandRunner"/> class.</summary>
    /// <param name="connection">The organization's DocumentRecord connection.</param>
    /// <param name="organizationId">The organization the store was found for.</param>
    /// <param name="tenantId">The organization's tenant.</param>
    /// <param name="logger">The logger.</param>
    public DocumentRecordCommandRunner(IDocumentRecordConnection connection, Guid organizationId, Guid tenantId, ILogger logger)
    {
        _connection = connection;
        _logger = logger;
        OrganizationId = organizationId;
        TenantId = tenantId;
    }

    /// <summary>Gets the organization the store was found for.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the organization's tenant.</summary>
    public Guid TenantId { get; }

    /// <summary>Sends a command for an organization, after checking it is the store's and the command has no problem.</summary>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="organizationId">The organization the caller named.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The connection's answer, or the refusal.</returns>
    public Task<IGenericResult<T>> Run<T>(Guid organizationId, IDocumentRecordCommand<T> command, CancellationToken cancellationToken) =>
        organizationId == OrganizationId
            ? Send(command, cancellationToken)
            : Task.FromResult(Refused<T>(command.CommandType, $"the store was found for organization '{OrganizationId}', not '{organizationId}'"));

    /// <summary>Sends a command for a tenant's view, or an organization's, after checking both are the store's.</summary>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="tenantId">The tenant the caller named.</param>
    /// <param name="organizationId">The organization the caller named, or null for the tenant's own view.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The connection's answer, or the refusal.</returns>
    public Task<IGenericResult<T>> Run<T>(Guid tenantId, Guid? organizationId, IDocumentRecordCommand<T> command, CancellationToken cancellationToken)
    {
        if (tenantId != TenantId)
        {
            return Task.FromResult(Refused<T>(command.CommandType, $"the store was found for tenant '{TenantId}', not '{tenantId}'"));
        }

        return organizationId is { } named && named != OrganizationId
            ? Task.FromResult(Refused<T>(command.CommandType, $"the store was found for organization '{OrganizationId}', not '{named}'"))
            : Send(command, cancellationToken);
    }

    private async Task<IGenericResult<T>> Send<T>(IDocumentRecordCommand<T> command, CancellationToken cancellationToken)
    {
        if (command.FirstProblem() is { } problem)
        {
            return Refused<T>(command.CommandType, problem);
        }

        DocumentRecordStoreLog.CommandSent(_logger, command.CommandType, OrganizationId);
        return await _connection.Execute(command, cancellationToken).ConfigureAwait(false);
    }

    private IGenericResult<T> Refused<T>(string command, string reason)
    {
        DocumentRecordStoreLog.CommandRefused(_logger, command, OrganizationId, reason);
        return GenericResult<T>.Failure(
            DocumentRecordResultCodes.ByName("DocumentRecordCommandRefused"),
            ResultDetails.Create("Command", command, "Reason", reason));
    }
}

using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Documents.Records;

/// <summary>
/// FDW's one document record store (FDW-3673): every seam carried out as document record commands on the DocumentRecord connection an
/// organization was assigned. The storage, its tables if it has any, and how each command is done are the connection's.
/// </summary>
/// <remarks>
/// It is made for one organization of one tenant, and refuses a call that names another: a connection several organizations share must never
/// be asked for another organization's records.
/// </remarks>
public sealed class ConnectionDocumentRecordStore : IDocumentRecordStore
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionDocumentRecordStore"/> class.</summary>
    /// <param name="connection">The organization's DocumentRecord connection, already found fit for the purpose.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="tenantId">The organization's tenant.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionDocumentRecordStore(IDocumentRecordConnection connection, Guid organizationId, Guid tenantId, ILogger<ConnectionDocumentRecordStore>? logger)
    {
        var runner = new DocumentRecordCommandRunner(connection, organizationId, tenantId, logger ?? NullLogger<ConnectionDocumentRecordStore>.Instance);
        Records = new ConnectionDocumentRecords(runner);
        Media = new ConnectionDocumentMedia(runner);
        Tree = new ConnectionRecordTree(runner);
        Attributes = new ConnectionAttributeValues(runner);
        StateHistory = new ConnectionDocumentStateHistory(runner);
        Requirements = new ConnectionRecordRequirements(runner);
        Reminders = new ConnectionReminderLog(runner);
        Revisions = new ConnectionOrganizationRevisions(runner);
        Targets = new ConnectionLinkTargets(runner);
        DocumentTypes = new ConnectionDocumentDefinitions<IDocumentTypeDefinition>(runner, (tenant, organization) => new ReadDocumentTypeDefinitionsCommand(tenant, organization));
        AttributeDefinitions = new ConnectionDocumentDefinitions<IAttributeDefinition>(runner, (tenant, organization) => new ReadAttributeDefinitionsCommand(tenant, organization));
        RecordKinds = new ConnectionDocumentDefinitions<IRecordKindDefinition>(runner, (tenant, organization) => new ReadRecordKindDefinitionsCommand(tenant, organization));
        WorkflowStages = new ConnectionDocumentDefinitions<IWorkflowStageDefinition>(runner, (tenant, organization) => new ReadWorkflowStageDefinitionsCommand(tenant, organization));
        RequirementRules = new ConnectionDocumentDefinitions<IRequirementRuleDefinition>(runner, (tenant, organization) => new ReadRequirementRuleDefinitionsCommand(tenant, organization));
        LinkKinds = new ConnectionDocumentDefinitions<ILinkKindDefinition>(runner, (tenant, organization) => new ReadLinkKindDefinitionsCommand(tenant, organization));
        Links = new ConnectionDocumentLinks(runner, LinkKinds, Targets);
    }

    /// <inheritdoc />
    public IDocumentRecords Records { get; }

    /// <inheritdoc />
    public IDocumentMedia Media { get; }

    /// <inheritdoc />
    public IRecordTree Tree { get; }

    /// <inheritdoc />
    public IAttributeValues Attributes { get; }

    /// <inheritdoc />
    public IDocumentStateHistory StateHistory { get; }

    /// <inheritdoc />
    public IRecordRequirements Requirements { get; }

    /// <inheritdoc />
    public IReminderLog Reminders { get; }

    /// <inheritdoc />
    public IOrganizationRevisions Revisions { get; }

    /// <inheritdoc />
    public IDocumentLinks Links { get; }

    /// <inheritdoc />
    public ILinkTargets Targets { get; }

    /// <inheritdoc />
    public IDocumentDefinitions<IDocumentTypeDefinition> DocumentTypes { get; }

    /// <inheritdoc />
    public IDocumentDefinitions<IAttributeDefinition> AttributeDefinitions { get; }

    /// <inheritdoc />
    public IDocumentDefinitions<IRecordKindDefinition> RecordKinds { get; }

    /// <inheritdoc />
    public IDocumentDefinitions<IWorkflowStageDefinition> WorkflowStages { get; }

    /// <inheritdoc />
    public IDocumentDefinitions<IRequirementRuleDefinition> RequirementRules { get; }

    /// <inheritdoc />
    public IDocumentDefinitions<ILinkKindDefinition> LinkKinds { get; }
}

using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Links;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// An organization's document records (FDW-3549, FDW-3673): the seams, carried out as document record commands on the DocumentRecord
/// connection the organization was assigned. FDW holds the one implementation; storage is the connection's. An organization reaches its
/// store through <see cref="IDocumentRecordStores.For"/>, never by injecting a seam.
/// </summary>
public interface IDocumentRecordStore
{
    /// <summary>Gets the organization's document records.</summary>
    IDocumentRecords Records { get; }

    /// <summary>Gets the organization's stored media.</summary>
    IDocumentMedia Media { get; }

    /// <summary>Gets the organization's record tree.</summary>
    IRecordTree Tree { get; }

    /// <summary>Gets the organization's attribute values.</summary>
    IAttributeValues Attributes { get; }

    /// <summary>Gets the organization's state history.</summary>
    IDocumentStateHistory StateHistory { get; }

    /// <summary>Gets the organization's record requirements.</summary>
    IRecordRequirements Requirements { get; }

    /// <summary>Gets the organization's reminder log.</summary>
    IReminderLog Reminders { get; }

    /// <summary>Gets the organization's per-organization revisions.</summary>
    IOrganizationRevisions Revisions { get; }

    /// <summary>Gets the organization's document links.</summary>
    IDocumentLinks Links { get; }

    /// <summary>Gets the organization's check of link targets.</summary>
    ILinkTargets Targets { get; }

    /// <summary>Gets the document type definitions that bear on the organization.</summary>
    IDocumentDefinitions<IDocumentTypeDefinition> DocumentTypes { get; }

    /// <summary>Gets the attribute definitions that bear on the organization.</summary>
    IDocumentDefinitions<IAttributeDefinition> AttributeDefinitions { get; }

    /// <summary>Gets the record kind definitions that bear on the organization.</summary>
    IDocumentDefinitions<IRecordKindDefinition> RecordKinds { get; }

    /// <summary>Gets the workflow stage definitions that bear on the organization.</summary>
    IDocumentDefinitions<IWorkflowStageDefinition> WorkflowStages { get; }

    /// <summary>Gets the requirement rule definitions that bear on the organization.</summary>
    IDocumentDefinitions<IRequirementRuleDefinition> RequirementRules { get; }

    /// <summary>Gets the link kind definitions that bear on the organization.</summary>
    IDocumentDefinitions<ILinkKindDefinition> LinkKinds { get; }
}

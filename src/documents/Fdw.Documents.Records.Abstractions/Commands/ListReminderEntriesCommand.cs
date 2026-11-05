using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Lists the reminders sent about a requirement, oldest first (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListReminderEntries", RestrictToCurrentCompilation = true)]
public sealed class ListReminderEntriesCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<IReminderEntry>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListReminderEntriesCommand()
        : base("ListReminderEntries")
    {
        OrganizationId = Guid.Empty;
        RequirementId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListReminderEntriesCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="requirementId">The requirement's public id.</param>
    public ListReminderEntriesCommand(Guid organizationId, Guid requirementId)
        : base("ListReminderEntries")
    {
        OrganizationId = organizationId;
        RequirementId = requirementId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the requirement's public id.</summary>
    public Guid RequirementId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), IdProblem(RequirementId, "RecordRequirement"));
}

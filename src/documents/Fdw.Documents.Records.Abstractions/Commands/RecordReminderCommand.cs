using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Records a reminder that was sent. A log entry changes no record, so it raises no revision; it answers true once recorded (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "RecordReminder", RestrictToCurrentCompilation = true)]
public sealed class RecordReminderCommand : DocumentRecordCommandBase<bool>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public RecordReminderCommand()
        : base("RecordReminder")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="RecordReminderCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="entry">The reminder.</param>
    public RecordReminderCommand(Guid organizationId, IReminderEntry? entry)
        : base("RecordReminder")
    {
        OrganizationId = organizationId;
        Entry = entry;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the reminder.</summary>
    public IReminderEntry? Entry { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Entry, entity => entity.Id, "ReminderEntry"));
}

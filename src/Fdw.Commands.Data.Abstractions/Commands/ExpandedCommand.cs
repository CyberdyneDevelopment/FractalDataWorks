namespace Fdw.Commands.Data.Abstractions;

/// <summary>One simpler command an expandable command expands into.</summary>
/// <param name="Command">The command to run.</param>
/// <param name="CountsAsAffected">
/// Whether the rows this command answers count toward the rows the expanded command affected. A delete that clears
/// the way for an insert does not: the rows written are the rows the insert answers.
/// </param>
/// <param name="AnswersRows">
/// Whether this command answers the rows it read or wrote (one dictionary each) and not a count. Only a command that
/// assembles its own result can expand into such a step.
/// </param>
public sealed record ExpandedCommand(IDataCommand Command, bool CountsAsAffected = true, bool AnswersRows = false);

using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// A command that writes one version of configuration: a save or a retire. It is a data command specialized for the
/// narrow domain of versioned configuration, and it expands into the plain data commands that do the write (a query of
/// the current rows, a close or a delete, an insert), so every kind of connection writes a version the same way.
/// </summary>
/// <typeparam name="TInput">The input the command carries: the record for a save, the field values to match for a retire.</typeparam>
/// <remarks>
/// A save and a retire share the rows they read and flag: the current rows a match selects, read with the referenced key
/// columns (the key values the row's children are filled from), and then closed (<c>IsCurrent</c> cleared) or deleted
/// (<c>IsDeleted</c> set, the row left current). Both stamp <c>ModifyDate</c>. Every name comes from the container's
/// configuration, and a value that is missing is refused by name; none is assumed.
/// </remarks>
public abstract class ConfigurationWriteCommand<TInput> : DataCommandBase<ConfigurationWriteResult, TInput>, IAssembledExpandableDataCommand<ConfigurationWriteResult>
{
    /// <summary>The column that is true on the newest version of a row.</summary>
    protected const string CurrentColumn = "IsCurrent";

    /// <summary>The column that is true on a row that has been deleted.</summary>
    protected const string DeletedColumn = "IsDeleted";

    /// <summary>The column stamped with the time a row was closed or deleted.</summary>
    protected const string ModifiedColumn = "ModifyDate";

    /// <summary>Initializes a new instance of the <see cref="ConfigurationWriteCommand{TInput}"/> class.</summary>
    /// <param name="commandType">The name of the command type.</param>
    /// <param name="input">The input the command carries.</param>
    protected ConfigurationWriteCommand(string commandType, TInput input)
        : base(commandType, input)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container);

    /// <inheritdoc/>
    public abstract IGenericResult<ConfigurationWriteResult> Assemble(IReadOnlyList<ExpansionAnswer> answers);

    /// <summary>The fields the Foreign keys referencing a container point at: the key values its children are filled from.</summary>
    /// <param name="container">The container written.</param>
    /// <returns>The referenced field names, none when nothing references the container; or why a referenced field cannot be resolved.</returns>
    protected IGenericResult<IReadOnlyList<string>> ReferencedFields(IDataContainer container)
    {
        var referencing = container.ReferencingKeys;
        if (referencing.IsFailure)
            return referencing.ToNewResult<IReadOnlyList<string>>();

        var fields = new List<string>();
        foreach (var binding in referencing.Value!)
        {
            foreach (var keyField in binding.Key.KeyFields)
            {
                if (keyField.ReferencedField is not { } referenced)
                    return GenericResult<IReadOnlyList<string>>.Failure(
                        ConfigurationWriteLog.ReferencedFieldNotResolved(NullLogger.Instance, CommandType, binding.Key.KeyName, binding.Owner.Name, container.Name));
                if (!fields.Contains(referenced.Name, StringComparer.Ordinal))
                    fields.Add(referenced.Name);
            }
        }

        return GenericResult<IReadOnlyList<string>>.Success(fields);
    }

    /// <summary>
    /// Makes the step that reads the current rows a match selects, answering the referenced key columns (or, when nothing
    /// references the container, the match columns, so there is still one row for each row written).
    /// </summary>
    /// <param name="match">The fields to match and their values.</param>
    /// <param name="returned">The referenced key columns.</param>
    /// <param name="notDeleted">Whether a row already deleted is left out, as it is for a delete.</param>
    /// <returns>The step; it answers rows (none for a container with nothing stored yet) and adds nothing to the rows affected.</returns>
    protected static ExpandedCommand ReadCurrentRows(IReadOnlyDictionary<string, object?> match, IReadOnlyList<string> returned, bool notDeleted)
        => new(
            new QueryCommand<Dictionary<string, object?>>
            {
                Filter = CurrentRows(match, notDeleted),
                ReadsForWrite = true,
                Projection = new ProjectionExpression
                {
                    Fields = (returned.Count > 0 ? returned : [.. match.Keys]).Select(c => new ProjectionField { PropertyName = c }).ToList(),
                },
            },
            CountsAsAffected: false,
            AnswersRows: true);

    /// <summary>
    /// Makes the step that closes (<c>IsCurrent</c> cleared) or deletes (<c>IsDeleted</c> set) the current rows a match selects,
    /// stamping <c>ModifyDate</c>, as a soft delete of the rows the filter selects. A delete selects only rows
    /// not already deleted, so <c>ModifyDate</c> keeps showing when a row was deleted; a close selects every
    /// current row, a deleted one too.
    /// </summary>
    /// <param name="match">The fields to match and their values.</param>
    /// <param name="column">The flag column: <c>IsCurrent</c> to close, <c>IsDeleted</c> to delete.</param>
    /// <param name="flagValue">The value the flag is set to: false to close, true to delete.</param>
    /// <returns>The step; it answers the rows it flagged.</returns>
    protected static ExpandedCommand FlagCurrentRows(IReadOnlyDictionary<string, object?> match, string column, bool flagValue)
        => new(new DeleteCommand
        {
            Filter = CurrentRows(match, notDeleted: flagValue),
            SoftDelete = new SoftDeleteOptions(column, SoftDeleteModes.FilterMatches) { FlagValue = flagValue, DeletedAtColumn = ModifiedColumn },
        });

    /// <summary>Makes a failed result of the steps from a message.</summary>
    /// <param name="message">Why the steps cannot be made.</param>
    /// <returns>The failed result.</returns>
    protected static IGenericResult<IReadOnlyList<ExpandedCommand>> Refused(Fdw.Messages.IGenericMessage message)
        => GenericResult<IReadOnlyList<ExpandedCommand>>.Failure(message);

    private static FilterExpression CurrentRows(IReadOnlyDictionary<string, object?> match, bool notDeleted)
    {
        var nodes = new List<IFilterNode>(match.Count + 2);
        foreach (var field in match)
            nodes.Add(Equal(field.Key, field.Value!));
        nodes.Add(Equal(CurrentColumn, true));
        if (notDeleted)
            nodes.Add(Equal(DeletedColumn, false));

        return new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes } };
    }

    private static FilterCondition Equal(string field, object value)
        => new() { PropertyName = field, Operator = FilterOperators.ByName("Equal"), Value = value };
}

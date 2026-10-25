using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql;

// What every SQL translator shares about a soft delete: the UPDATE it runs in place of a DELETE, and the refusal of an Update
// that carries soft-delete options. How a mode picks its rows is the mode's own (ISoftDeleteMode); a translator for a kind that
// has a statement of its own for a soft delete (MsSql) does not call this.
public abstract partial class SqlDataCommandTranslatorBase<TCommand>
{
    private const string SoftDeleteFlagParameter = "fdw_sd_flag";
    private const string SoftDeleteConditionParameter = "fdw_sd_match";

    /// <summary>
    /// Builds the UPDATE a soft delete runs in place of a DELETE: set the deleted-flag column (and the deleted-at column when named),
    /// skip rows already holding the flag value, and take the rows from the command's filter and the conditions its mode adds.
    /// </summary>
    /// <param name="table">The quoted, qualified table name.</param>
    /// <param name="dialect">The dialect that quotes identifiers and names parameters.</param>
    /// <param name="options">The soft-delete options the command carries.</param>
    /// <param name="container">The container the command writes to; every column the options name must be declared on it.</param>
    /// <param name="now">The expression that gives the database's current time, stamped on the deleted-at column.</param>
    /// <param name="filterPredicate">The command's WHERE predicate, or <see langword="null"/> when it has no filter.</param>
    /// <param name="addParameter">Adds a named parameter to the command being built.</param>
    /// <returns>The UPDATE statement.</returns>
    /// <exception cref="InvalidOperationException">The options are inconsistent, name a column the container does not declare, or need a source set this statement cannot stage.</exception>
    protected static string BuildSoftDeleteUpdate(
        string table,
        ISqlDialect dialect,
        SoftDeleteOptions options,
        IStorageContainer container,
        string now,
        string? filterPredicate,
        Action<string, object?> addParameter)
    {
        var problem = options.FirstProblem() ?? SoftDeleteColumnProblem(options, container);
        if (problem is null && options.Mode.NeedsSourceSet)
            problem = $"flagging the keys absent from a source set is not supported by the {dialect.Name} dialect";
        if (problem is not null)
            throw new InvalidOperationException($"The soft delete on container {container.Name} is refused: {problem}");

        var p = dialect.ParameterPrefix;
        var flag = dialect.QuoteIdentifier(options.DeletedFlagColumn);
        addParameter(SoftDeleteFlagParameter, options.FlagValue);

        var assignments = new List<string> { $"{flag} = {p}{SoftDeleteFlagParameter}" };
        if (options.DeletedAtColumn is not null)
            assignments.Add($"{dialect.QuoteIdentifier(options.DeletedAtColumn)} = {now}");

        var predicates = new List<string>();
        if (filterPredicate is not null)
            predicates.Add($"({filterPredicate})");

        var conditions = options.Mode.Conditions(options);
        for (var i = 0; i < conditions.Count; i++)
        {
            var parameter = $"{SoftDeleteConditionParameter}{i}";
            addParameter(parameter, conditions[i].Value);
            predicates.Add($"{dialect.QuoteIdentifier(conditions[i].Column)} = {p}{parameter}");
        }

        predicates.Add($"({flag} IS NULL OR {flag} <> {p}{SoftDeleteFlagParameter})");
        return $"UPDATE {table} SET {string.Join(", ", assignments)} WHERE {string.Join(" AND ", predicates)}";
    }

    /// <summary>Refuses an Update that carries soft-delete options, which a SQL kind without a statement for it cannot apply to an Update.</summary>
    /// <param name="command">The command being translated.</param>
    /// <param name="container">The container the command writes to.</param>
    /// <exception cref="InvalidOperationException">The command carries soft-delete options.</exception>
    protected static void RefuseSoftDeleteOnUpdate(IDataCommand command, IStorageContainer container)
    {
        if (command is ISoftDeletableCommand { SoftDelete: not null })
            throw new InvalidOperationException($"An update of container {container.Name} cannot also soft delete on this connection; send a delete with soft-delete options");
    }

    private static string? SoftDeleteColumnProblem(SoftDeleteOptions options, IStorageContainer container)
    {
        var declared = container.Schema.Fields;
        var names = new List<string> { options.DeletedFlagColumn };
        if (options.DeletedAtColumn is not null)
            names.Add(options.DeletedAtColumn);
        names.AddRange(options.Mode.Conditions(options).Select(c => c.Column));

        var unknown = names.FirstOrDefault(n => !declared.Any(f => string.Equals(f.Name, n, StringComparison.OrdinalIgnoreCase)));
        if (unknown is not null)
            return $"container {container.Name} does not declare column '{unknown}'";

        var computed = names.FirstOrDefault(n => declared.Any(f => f.IsComputed && string.Equals(f.Name, n, StringComparison.OrdinalIgnoreCase)));
        if (computed is not null)
            return $"column '{computed}' is computed by the database and cannot be a soft-delete column";

        return declared.Any(f => f.IsIdentity && string.Equals(f.Name, options.DeletedFlagColumn, StringComparison.OrdinalIgnoreCase))
            ? $"column '{options.DeletedFlagColumn}' is an identity column and cannot be the deleted-flag column"
            : null;
    }
}

using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Services.Data;

/// <summary>
/// The write a data set with its own filters sends its source (FDW-5069): narrowed to the rows the filters keep, so a write through the data
/// set never touches a row outside it.
/// </summary>
/// <remarks>
/// <para>
/// The data set's filters (<see cref="DataSetFilters"/>) are put in the source's names, as a read puts them. A command that can be narrowed
/// (<see cref="IScopeNarrowableCommand"/>: an update, a delete) is sent narrowed. Each row of a command that carries rows
/// (<see cref="IRowInputCommand"/>: an insert) is checked against them before anything is sent, and one outside them refuses the whole write.
/// Any other write (a bulk update or delete, an upsert, whose rows are chosen inside the connection) cannot be narrowed here and is refused,
/// never sent unscoped.
/// </para>
/// <para>A data set with no filters sends every write unchanged.</para>
/// </remarks>
internal static class DataSetWriteScope
{
    /// <summary>Gets the write to send the source.</summary>
    /// <param name="context">The data set written through.</param>
    /// <param name="command">The write.</param>
    /// <param name="fieldMappings">The source's logical-to-physical names, if it maps any.</param>
    /// <param name="sourceName">The source, for the messages.</param>
    /// <returns>The write to send, or the refusal of a write the data set's filters do not allow.</returns>
    public static IGenericResult<IDataCommand> For(
        DataSetExecutionContext context, IDataCommand command, IReadOnlyDictionary<string, string>? fieldMappings, string sourceName)
    {
        var own = DataSetFilters.Expression(context.Config, context.Logger);
        if (own.IsFailure)
            return own.ToNewResult<IDataCommand>();
        if (own.Value is not { Root: not null } filters)
            return GenericResult<IDataCommand>.Success(command);

        var scope = context.Pushdown.TranslateToPhysical(filters, fieldMappings);
        if (scope.IsFailure)
            return GenericResult<IDataCommand>.Failure(DataGatewayLogger.FilterTranslationFailed(context.Logger, sourceName, scope.FailureReason().Message));
        if (scope.Value is not { Root: { } root } physical)
            return GenericResult<IDataCommand>.Success(command);

        if (command is IScopeNarrowableCommand narrowable)
            return GenericResult<IDataCommand>.Success(narrowable.NarrowedTo(physical));

        if (command is IRowInputCommand rows)
            return RowsInside(context, rows, root);

        return GenericResult<IDataCommand>.Failure(
            DataGatewayLogger.FilteredDataSetWriteNotScopable(context.Logger, context.Config.Name, command.CommandType));
    }

    // Each row the command writes, read as its values in the source's names, must satisfy the data set's filters; the command is sent
    // unchanged when every row does.
    private static IGenericResult<IDataCommand> RowsInside(DataSetExecutionContext context, IRowInputCommand command, IFilterNode scope)
    {
        var index = 0;
        foreach (var row in command.InputRows)
        {
            if (row is null)
                return GenericResult<IDataCommand>.Failure(
                    DataGatewayLogger.FilteredDataSetInsertRowUnreadable(context.Logger, context.Config.Name, index, "the row is null"));

            var values = row is DataRecord record
                ? GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(record.ToDictionary(), StringComparer.OrdinalIgnoreCase))
                : DataSetExecutionHelpers.ObjectToDictionary(row);
            if (values.IsFailure)
                return GenericResult<IDataCommand>.Failure(
                    DataGatewayLogger.FilteredDataSetInsertRowUnreadable(context.Logger, context.Config.Name, index, values.FailureReason().Message));
            if (values.Value is not { } read || !InMemoryQuery.Matches(read, scope))
                return GenericResult<IDataCommand>.Failure(
                    DataGatewayLogger.FilteredDataSetInsertRowOutsideFilter(context.Logger, context.Config.Name, index));

            index++;
        }

        return GenericResult<IDataCommand>.Success(command);
    }
}

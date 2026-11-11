using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.Logging;
using Fdw.Commands.Data.Updates;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// Checks a set-based write's columns against its target (FDW-4953, FDW-4954): every column is one the container declares, and
/// no column a bulk update sets is one the database computes or numbers itself.
/// </summary>
public static class SetBasedColumns
{
    /// <summary>Describes the first thing wrong with a bulk update's options for the container, or returns <see langword="null"/>.</summary>
    /// <param name="options">The bulk update's options.</param>
    /// <param name="container">The target.</param>
    /// <returns>The refusal, or <see langword="null"/>.</returns>
    public static IGenericMessage? Problem(BulkUpdateOptions options, IStorageContainer container)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(container);
        var general = options.FirstProblem();
        if (general is not null)
            return general;

        var unknown = Undeclared(container, options.StagedColumns);
        if (unknown is not null)
            return CommandOptionsLog.BulkUpdateOptionsInconsistent(NullLogger.Instance, unknown);

        var unsettable = options.SetColumns.FirstOrDefault(column => Field(container, column) is { } field && (field.IsIdentity || field.IsComputed));
        return unsettable is null
            ? null
            : CommandOptionsLog.BulkUpdateOptionsInconsistent(NullLogger.Instance, $"column '{unsettable}' is numbered or computed by the database and cannot be written");
    }

    /// <summary>Describes the first thing wrong with a bulk delete's options for the container, or returns <see langword="null"/>.</summary>
    /// <param name="options">The bulk delete's options.</param>
    /// <param name="container">The target.</param>
    /// <returns>The refusal, or <see langword="null"/>.</returns>
    public static IGenericMessage? Problem(BulkDeleteOptions options, IStorageContainer container)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(container);
        var general = options.FirstProblem();
        if (general is not null)
            return general;

        var unknown = Undeclared(container, options.KeyColumns);
        return unknown is null ? null : CommandOptionsLog.BulkDeleteOptionsInconsistent(NullLogger.Instance, unknown);
    }

    /// <summary>Gives the name the container declares for a column, matched ignoring case.</summary>
    /// <param name="container">The target.</param>
    /// <param name="column">The column as the options name it; the options' <c>Problem</c> check has found the container declares it.</param>
    /// <returns>The declared name.</returns>
    public static string Declared(IStorageContainer container, string column)
    {
        ArgumentNullException.ThrowIfNull(container);
        return Field(container, column) is { } field
            ? field.Name
            : throw new InvalidOperationException($"Container {container.Name} does not declare column '{column}'");
    }

    /// <summary>Lists the declared names of <paramref name="columns"/>, in order.</summary>
    /// <param name="container">The target.</param>
    /// <param name="columns">The columns as the options name them.</param>
    /// <returns>The declared names.</returns>
    public static IReadOnlyList<string> Declared(IStorageContainer container, IReadOnlyList<string> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        return columns.Select(column => Declared(container, column)).ToList();
    }

    private static IField? Field(IStorageContainer container, string column)
        => container.Schema.Fields.FirstOrDefault(field => string.Equals(field.Name, column, StringComparison.OrdinalIgnoreCase));

    // Why the column is refused, naming it, or null when the container declares every column.
    private static string? Undeclared(IStorageContainer container, IReadOnlyList<string> columns)
        => columns.FirstOrDefault(column => Field(container, column) is null) is { } unknown
            ? $"container {container.Name} does not declare column '{unknown}'"
            : null;
}

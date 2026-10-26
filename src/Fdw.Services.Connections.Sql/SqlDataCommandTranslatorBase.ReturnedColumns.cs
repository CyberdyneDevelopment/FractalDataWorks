using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Sql;

// What every SQL translator shares about the columns an Insert or Update asks to have returned.
public abstract partial class SqlDataCommandTranslatorBase<TCommand>
{
    /// <summary>
    /// Gets the declared names of the columns the command asks to return, in the order it names them, so every SQL kind names and
    /// refuses them the same way.
    /// </summary>
    /// <param name="command">The data command.</param>
    /// <param name="container">The container the command writes to.</param>
    /// <returns>The declared column names; empty when the command asks for none.</returns>
    /// <exception cref="InvalidOperationException">The command names a column the container does not declare, or names one twice.</exception>
    protected static IReadOnlyList<string> ReturnedColumnsOf(IDataCommand command, IStorageContainer container)
    {
        if (command is not IReturningCommand { ReturnColumns: { Count: > 0 } requested })
            return [];

        var declared = container.Schema.Fields;
        var names = new List<string>(requested.Count);
        foreach (var column in requested)
        {
            var field = declared.FirstOrDefault(f => string.Equals(f.Name, column, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Container {container.Name} does not declare column '{column}', which the command asks to return");
            if (names.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"The command asks to return column '{field.Name}' of container {container.Name} more than once");
            names.Add(field.Name);
        }

        return names;
    }
}

using System;
using System.Collections.Generic;
using Fdw.Tds.Endpoints.Sql.Expressions;

namespace Fdw.Tds.Endpoints.Sql;

/// <summary>
/// The variables of one batch: declared with DECLARE, or bound from an RPC's parameters.
/// </summary>
/// <remarks>
/// A variable keeps the type it was declared with, and every assignment converts to it, as SQL Server's
/// does. Names compare without case, and the leading @ is part of the name.
/// </remarks>
public sealed class SqlVariables
{
    private readonly Dictionary<string, Slot> _slots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether a variable is declared.</summary>
    /// <param name="name">The name, with its leading @.</param>
    /// <returns>True when declared.</returns>
    public bool IsDeclared(string name) => _slots.ContainsKey(name);

    /// <summary>Declares a variable.</summary>
    /// <param name="name">The name, with its leading @.</param>
    /// <param name="type">The type assignments convert to.</param>
    /// <param name="value">The initial value, already of that type; null for NULL.</param>
    public void Declare(string name, SqlConversionTarget type, object? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        _slots[name] = new Slot(type, value);
    }

    /// <summary>Gets a variable's value.</summary>
    /// <param name="name">The name, with its leading @.</param>
    /// <param name="value">The value; null for NULL.</param>
    /// <returns>True when the variable is declared.</returns>
    public bool TryGet(string name, out object? value)
    {
        if (_slots.TryGetValue(name, out var slot))
        {
            value = slot.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Gets the type a variable was declared with.</summary>
    /// <param name="name">The name, with its leading @.</param>
    /// <param name="type">The type.</param>
    /// <returns>True when the variable is declared.</returns>
    public bool TryGetType(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SqlConversionTarget? type)
    {
        if (_slots.TryGetValue(name, out var slot))
        {
            type = slot.Type;
            return true;
        }

        type = null;
        return false;
    }

    /// <summary>Sets a declared variable's value, already converted to its type.</summary>
    /// <param name="name">The name, with its leading @.</param>
    /// <param name="value">The value; null for NULL.</param>
    /// <returns>True when the variable is declared.</returns>
    public bool TrySet(string name, object? value)
    {
        if (!_slots.TryGetValue(name, out var slot))
            return false;

        _slots[name] = slot with { Value = value };
        return true;
    }

    private sealed record Slot(SqlConversionTarget Type, object? Value);
}

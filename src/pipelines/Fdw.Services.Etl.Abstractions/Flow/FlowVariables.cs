using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Named values in scope while a pipeline's steps run (FDW-1403, FDW-1549): a scope with an optional parent, declared before it is set.
/// </summary>
/// <remarks>
/// <para>
/// A name is declared in one scope and visible in that scope and every child scope. A set finds the scope that declared the name and
/// changes it there, so a loop body can move an outer variable on. Setting a name nobody declared is refused, never created: a
/// misspelt name must not become a second variable.
/// </para>
/// <para>
/// A name is a variable or a parameter. A parameter is declared from what the run was started with, and is read-only (a set is
/// refused) or mutable (a set changes it, like a variable). A parameter the run left blank is declared with no value: it is in scope,
/// <see cref="IsBlank"/> says so, <see cref="TryGet"/> finds nothing for it, and a set on a mutable one gives it its first value.
/// </para>
/// <para>
/// Names compare ignoring case. Reads and writes are safe from several tasks.
/// </para>
/// </remarks>
public sealed class FlowVariables
{
    private readonly FlowVariables? _parent;
    private readonly Dictionary<string, Entry> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <summary>Initializes a new instance of the <see cref="FlowVariables"/> class with no parent.</summary>
    public FlowVariables()
    {
    }

    private FlowVariables(FlowVariables parent)
    {
        _parent = parent;
    }

    /// <summary>Makes a child scope: it sees this scope's names and can declare its own.</summary>
    /// <returns>The child scope.</returns>
    public FlowVariables CreateChild() => new(this);

    /// <summary>Declares a variable in this scope.</summary>
    /// <param name="name">The name; not already declared in this scope.</param>
    /// <param name="value">The starting value.</param>
    /// <exception cref="InvalidOperationException">The name is already declared in this scope.</exception>
    public void Declare(string name, object? value) => Add(name, new Entry(value, false, false));

    /// <summary>Declares a parameter in this scope with the value the run was started with.</summary>
    /// <param name="name">The name; not already declared in this scope.</param>
    /// <param name="value">The value the run supplied.</param>
    /// <param name="isReadOnly">Whether a set on the parameter is refused.</param>
    /// <exception cref="InvalidOperationException">The name is already declared in this scope.</exception>
    public void DeclareParameter(string name, object? value, bool isReadOnly) => Add(name, new Entry(value, isReadOnly, false));

    /// <summary>Declares a parameter the run left blank: in scope, with no value.</summary>
    /// <param name="name">The name; not already declared in this scope.</param>
    /// <param name="isReadOnly">Whether a set on the parameter is refused; a blank read-only parameter stays blank.</param>
    /// <exception cref="InvalidOperationException">The name is already declared in this scope.</exception>
    public void DeclareBlankParameter(string name, bool isReadOnly) => Add(name, new Entry(null, isReadOnly, true));

    /// <summary>Reads a name from this scope or a parent.</summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value when the name is declared and has one.</param>
    /// <returns>Whether the name is declared in scope with a value; false for a name nobody declared and for a blank parameter.</returns>
    public bool TryGet(string name, out object? value)
    {
        lock (_gate)
        {
            if (_values.TryGetValue(name, out var entry))
            {
                value = entry.Value;
                return !entry.IsBlank;
            }
        }

        if (_parent is not null)
        {
            return _parent.TryGet(name, out value);
        }

        value = null;
        return false;
    }

    /// <summary>Says whether a name is declared in this scope or a parent, with a value or not.</summary>
    /// <param name="name">The name.</param>
    /// <returns>Whether the name is declared in scope.</returns>
    public bool IsDeclared(string name)
    {
        lock (_gate)
        {
            if (_values.ContainsKey(name))
            {
                return true;
            }
        }

        return _parent is not null && _parent.IsDeclared(name);
    }

    /// <summary>Says whether a name is a parameter the run left blank.</summary>
    /// <param name="name">The name.</param>
    /// <returns>Whether the name is declared in scope and has no value.</returns>
    public bool IsBlank(string name)
    {
        lock (_gate)
        {
            if (_values.TryGetValue(name, out var entry))
            {
                return entry.IsBlank;
            }
        }

        return _parent is not null && _parent.IsBlank(name);
    }

    /// <summary>Says whether a name is a read-only parameter.</summary>
    /// <param name="name">The name.</param>
    /// <returns>Whether the name is declared in scope as a parameter a set is refused on.</returns>
    public bool IsReadOnly(string name)
    {
        lock (_gate)
        {
            if (_values.TryGetValue(name, out var entry))
            {
                return entry.IsReadOnly;
            }
        }

        return _parent is not null && _parent.IsReadOnly(name);
    }

    /// <summary>Changes a declared name, in the scope that declared it.</summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The new value.</param>
    /// <returns>Whether the name was changed; false when nobody declared it in scope and when it is a read-only parameter (<see cref="IsReadOnly"/> tells them apart).</returns>
    public bool TrySet(string name, object? value)
    {
        lock (_gate)
        {
            if (_values.TryGetValue(name, out var entry))
            {
                if (entry.IsReadOnly)
                {
                    return false;
                }

                _values[name] = new Entry(value, false, false);
                return true;
            }
        }

        return _parent is not null && _parent.TrySet(name, value);
    }

    /// <summary>Copies every name in scope, a nearer scope's value winning over a farther one's; a blank parameter copies as null.</summary>
    /// <returns>The copy, keyed ignoring case.</returns>
    public Dictionary<string, object?> Snapshot()
    {
        var copy = _parent is null ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) : _parent.Snapshot();
        lock (_gate)
        {
            foreach (var pair in _values)
            {
                copy[pair.Key] = pair.Value.Value;
            }
        }

        return copy;
    }

    private void Add(string name, Entry entry)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A variable has a name.", nameof(name));
        }

        lock (_gate)
        {
            if (_values.ContainsKey(name))
            {
                throw new InvalidOperationException($"Variable '{name}' is already declared in this scope.");
            }

            _values.Add(name, entry);
        }
    }

    private sealed class Entry
    {
        internal Entry(object? value, bool isReadOnly, bool isBlank)
        {
            Value = value;
            IsReadOnly = isReadOnly;
            IsBlank = isBlank;
        }

        internal object? Value { get; }

        internal bool IsReadOnly { get; }

        internal bool IsBlank { get; }
    }
}

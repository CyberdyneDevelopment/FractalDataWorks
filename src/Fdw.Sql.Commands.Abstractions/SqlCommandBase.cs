using System;
using Fdw.Collections;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Base class for every SQL command. Derives from <see cref="TypeOptionBase{TId,TBase}"/>.</summary>
public abstract class SqlCommandBase : TypeOptionBase<int, SqlCommandBase>, ISqlCommand
{
    /// <inheritdoc/>
    public ISqlCommandCategory? CommandCategory { get; }

    /// <summary>Used by TypeCollection for the Empty sentinel.</summary>
    protected SqlCommandBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, "SqlCommand")
    {
        CommandCategory = null;
    }

    /// <summary>Initializes a new command instance.</summary>
    protected SqlCommandBase(string name, ISqlCommandCategory category, string description)
        : base(GenerateIdFromName(name), name, name, name, description, "SqlCommand")
    {
        CommandCategory = category ?? throw new ArgumentNullException(nameof(category));
    }

    /// <summary>Deterministic FNV-1a hash from name → command id.</summary>
    private static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;
            int hash = FnvOffsetBasis;
            foreach (char c in name) { hash ^= c; hash *= FnvPrime; }
            return hash & 0x7FFFFFFF; // positive
        }
    }
}

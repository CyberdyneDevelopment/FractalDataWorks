using System;
using Fdw.Collections;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Base class for every SQL command. Derives from <see cref="TypeOptionBase{TBase}"/>.</summary>
public abstract class SqlCommandBase : TypeOptionBase<SqlCommandBase>, ISqlCommand
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
        : base(name, name, name, description, "SqlCommand")
    {
        CommandCategory = category ?? throw new ArgumentNullException(nameof(category));
    }
}

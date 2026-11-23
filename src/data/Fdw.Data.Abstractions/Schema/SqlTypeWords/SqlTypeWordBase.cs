using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>The base of every SQL type word.</summary>
[ExcludeFromCodeCoverage]
public abstract class SqlTypeWordBase : TypeOptionBase<SqlTypeWordBase>, ISqlTypeWord
{
    /// <summary>Initializes a new instance of the <see cref="SqlTypeWordBase"/> class.</summary>
    /// <param name="name">The type's name.</param>
    /// <param name="sqlType">The SQL type.</param>
    /// <param name="words">The words a field type name may state this type by, in lower case.</param>
    protected SqlTypeWordBase(string name, SqlDbType sqlType, string[] words)
        : base(name)
    {
        SqlType = sqlType;
        Words = words;
    }

    /// <inheritdoc />
    public SqlDbType SqlType { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Words { get; }
}

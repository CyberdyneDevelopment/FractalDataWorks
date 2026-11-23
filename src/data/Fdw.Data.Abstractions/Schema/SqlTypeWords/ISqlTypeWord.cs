using System.Collections.Generic;
using System.Data;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>One SQL column type, with the words a field type is named by that map to it.</summary>
public interface ISqlTypeWord : ITypeOption<int, SqlTypeWordBase>
{
    /// <summary>Gets the SQL type.</summary>
    SqlDbType SqlType { get; }

    /// <summary>Gets the words a field type name may state this type by, in lower case.</summary>
    IReadOnlyList<string> Words { get; }
}

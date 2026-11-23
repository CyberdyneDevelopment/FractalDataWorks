using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>The type the vector reader collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class MsSqlVectorReaderBase : TypeOptionBase<MsSqlVectorReaderBase>, IMsSqlVectorReader
{
    /// <summary>Initializes the reader with its name and the name of the value type it reads.</summary>
    /// <param name="name">The reader's name.</param>
    /// <param name="valueTypeName">The name of the type of value it reads, as the value's type reports it.</param>
    protected MsSqlVectorReaderBase(string name, string valueTypeName)
        : base(name)
    {
        ValueTypeName = valueTypeName;
    }

    /// <inheritdoc/>
    [TypeLookup("ByValueTypeName")]
    public string ValueTypeName { get; }

    /// <inheritdoc/>
    public abstract object? Read(object dbValue);
}

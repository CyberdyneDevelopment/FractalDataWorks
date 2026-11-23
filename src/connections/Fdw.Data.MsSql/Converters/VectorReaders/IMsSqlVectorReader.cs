using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>Reads one type of value SqlClient returns for a <c>vector</c> column as its <c>float[]</c>; the collection is found by the value type's name (<c>ByValueTypeName</c>).</summary>
public interface IMsSqlVectorReader : ITypeOption<int, IMsSqlVectorReader>
{
    /// <summary>Gets the name of the type of value this reader reads (<c>Single[]</c>), the name SqlClient's value reports.</summary>
    [TypeLookup("ByValueTypeName")]
    string ValueTypeName { get; }

    /// <summary>Reads the value.</summary>
    /// <param name="dbValue">The value, of the type this reader is named for.</param>
    /// <returns>The vector, or <see langword="null"/> for a null vector.</returns>
    object? Read(object dbValue);
}

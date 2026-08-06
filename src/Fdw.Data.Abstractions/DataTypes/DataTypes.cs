using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// TypeCollection of portable abstract data types that normalize storage-native types
/// across all connection backends (SQL Server, PostgreSQL, HTTP, etc.).
/// </summary>
/// <remarks>
/// Each <see cref="DataTypeBase"/> instance in this collection corresponds to one abstract
/// type. Storage-specific implementations map their native types (e.g., SQL Server
/// <c>bigint</c> → <c>DataTypes.Int64</c>) at the connection layer, keeping the
/// DataSet model portable.
/// </remarks>
[TypeCollection(typeof(DataTypeBase), typeof(IDataType), typeof(DataTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class DataTypes : TypeCollectionBase<DataTypeBase, IDataType>
{
    // Source generator produces:
    //   - Static constructor
    //   - Static property for each [TypeOption] (Int64, Int32, String, ...)
    //   - All() / ByName() / ById() / NotFound() methods
}

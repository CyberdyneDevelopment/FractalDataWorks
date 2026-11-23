using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>
/// The types a <c>vector</c> column can come back as (<c>SqlVector&lt;float&gt;</c>, <c>float[]</c>, its JSON text), found by the value type's name (<c>ByValueTypeName</c>).
/// A value of another type is not a vector, and the read fails.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(MsSqlVectorReaderBase), typeof(IMsSqlVectorReader), typeof(MsSqlVectorReaders))]
public abstract partial class MsSqlVectorReaders : TypeCollectionBase<MsSqlVectorReaderBase, IMsSqlVectorReader>
{
}

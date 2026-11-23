using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>
/// The value types a bulk copy fits to a date column (<c>DateTime</c>, <c>DateTimeOffset</c>, <c>DateOnly</c>, <c>String</c>), found by the value type's name.
/// A value of another type has no reader and is not a date.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(MsSqlBulkCopyDateReaderBase), typeof(IMsSqlBulkCopyDateReader), typeof(MsSqlBulkCopyDateReaders))]
public abstract partial class MsSqlBulkCopyDateReaders : TypeCollectionBase<MsSqlBulkCopyDateReaderBase, IMsSqlBulkCopyDateReader>
{
}

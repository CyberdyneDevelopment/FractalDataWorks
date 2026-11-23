using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RowQuery.DateValues;

/// <summary>The date and time types a record row can compare (<c>DateTime</c>, <c>DateTimeOffset</c>), found by the value type's name.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DateValueReaderBase), typeof(IDateValueReader), typeof(DateValueReaders))]
public abstract partial class DateValueReaders : TypeCollectionBase<DateValueReaderBase, IDateValueReader>
{
}

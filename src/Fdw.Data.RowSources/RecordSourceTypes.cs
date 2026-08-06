using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources;

/// <summary>
/// TypeCollection for record source types (DataReader, Xml, Json, Delimited, FixedWidth, Http).
/// The factory: <c>RecordSourceTypes.ByName(format).Create(context)</c> builds a reader from a
/// container's configuration.
/// </summary>
/// <remarks>
/// Record sources are adapters around existing stream/reader instances, so they're TypeCollection
/// members (not ServiceTypes) — no DI needed. (Renamed from <c>RowSourceTypes</c>: it produces RECORD
/// sources — items or rows.)
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RecordSourceTypeBase), typeof(IRecordSourceType), typeof(RecordSourceTypes))]
public abstract partial class RecordSourceTypes : TypeCollectionBase<RecordSourceTypeBase, IRecordSourceType>
{
}

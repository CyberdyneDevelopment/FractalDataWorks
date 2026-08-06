using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources;

/// <summary>
/// TypeCollection for record writer types (Json, Xml, Delimited, FixedWidth). The write-side mirror of
/// <see cref="RecordSourceTypes"/>: <c>RecordWriterTypes.ByName(format).Create(context)</c> builds a
/// writer from a container's configuration.
/// </summary>
/// <remarks>
/// Record writers are adapters around a target <c>TextWriter</c>, so they're TypeCollection members
/// (not ServiceTypes) — no DI needed. (Renamed from <c>RowWriterTypes</c>: it produces RECORD writers —
/// items or rows.)
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RecordWriterTypeBase), typeof(IRecordWriterType), typeof(RecordWriterTypes))]
public abstract partial class RecordWriterTypes : TypeCollectionBase<RecordWriterTypeBase, IRecordWriterType>
{
}

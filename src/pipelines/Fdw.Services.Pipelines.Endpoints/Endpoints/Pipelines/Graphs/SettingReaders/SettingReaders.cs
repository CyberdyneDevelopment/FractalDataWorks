using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The readers of the kinds of setting a graph request states other than a single value: a column list, the field maps and input mappings, the split
/// outputs, the value sources and the parameters.
/// </summary>
/// <remarks>A setting is read by the reader of its kind, found through <c>ByKind</c>; a kind that matches none gets <c>NotFound</c>, and the setting is read as a single value.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SettingReaderBase), typeof(ISettingReader), typeof(SettingReaders))]
public abstract partial class SettingReaders : TypeCollectionBase<SettingReaderBase, ISettingReader>
{
}
